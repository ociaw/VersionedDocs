using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Statiq.CodeAnalysis;
using Statiq.Common;
using Statiq.Core;
using Statiq.Docs;
using Statiq.Docs.Pipelines;
using Statiq.Web;

namespace Ociaw.VersionedDocs;

/// <summary>
/// Uses Roslyn to analyze any source files loaded in the previous
/// pipeline along with any specified assemblies. This pipeline
/// results in documents that represent Roslyn symbols.
/// </summary>
public sealed class VersionedApi : Pipeline
{
    /// <summary>
    /// Creates a new instance of the <see cref="VersionedApi"/> pipeline.
    /// </summary>
    public VersionedApi()
    {
        Dependencies.AddRange(nameof(Code), nameof(Vcs));

        // Will only get processed by the Content pipeline if OutputApiDocuments is true which sets
        // ContentType to ContentType.Content, otherwise pipeline will ignore output documents
        DependencyOf.Add(nameof(Statiq.Web.Pipelines.Content));

        ProcessModules = new ModuleList(
            new ConcatDocuments(nameof(Code)),
            new ConcatDocuments(nameof(Vcs)),
            new GroupDocuments(VcsKeys.RevisionName),
            new CacheDocuments(
                new ExecuteOverChildDocuments(
                    new CopyMetadataAfterChildExecution(VcsKeys.RevisionName, VcsKeys.RevisionId, VcsKeys.RevisionTimestamp,
                        new ExecuteConfig(Config.FromContext(ctx =>
                            new AnalyzeCSharp()
                                .WhereNamespaces(ctx.Settings.GetBool(DocsKeys.IncludeGlobalNamespace))
                                .WherePublic()
                                .WithCssClasses("code", "cs")
                                .WithDestinationPrefix(ctx.GetPath(DocsKeys.ApiPath).Combine(ctx.Inputs.First().GetString(VcsKeys.RevisionName)))
                                .WithAssemblies(Config.FromContext<IEnumerable<String>>(c => c.GetList<String>(DocsKeys.AssemblyFiles)))
                                .WithProjects(Config.FromContext<IEnumerable<String>>(c => c.GetList<String>(DocsKeys.ProjectFiles)))
                                .WithSolutions(Config.FromContext<IEnumerable<String>>(c => c.GetList<String>(DocsKeys.SolutionFiles)))
                                .WithAssemblySymbols()
                                .WithImplicitInheritDoc(ctx.GetBool(DocsKeys.ImplicitInheritDoc))
                    ))),
                    new ExecuteConfig(Config.FromDocument((doc, ctx) =>
                    {
                        // Calculate a type name to link lookup for auto linking
                        String? name = null;
                        String kind = doc.GetString(CodeAnalysisKeys.Kind);
                        if (kind is "NamedType")
                        {
                            name = doc.GetString(CodeAnalysisKeys.DisplayName);
                        }
                        else if (kind is "Property" or "Method")
                        {
                            IDocument containingType = doc.GetDocument(CodeAnalysisKeys.ContainingType);
                            if (containingType != null)
                            {
                                name = $"{containingType.GetString(CodeAnalysisKeys.DisplayName)}.{doc.GetString(CodeAnalysisKeys.DisplayName)}";
                            }
                        }
                        else if (kind is "Revision")
                        {
                            name = doc.GetString(VcsKeys.RevisionName);
                        }

                        if (name != null)
                        {
                            TypeNameLinks typeNameLinks = ctx.GetRequiredService<TypeNameLinks>();
                            typeNameLinks.Links.AddOrUpdate(WebUtility.HtmlEncode(name), ctx.GetLink(doc), (_, _) => String.Empty);
                        }

                        String revision = doc.GetString(VcsKeys.RevisionName);

                        // Add metadata
                        MetadataItems metadataItems = new MetadataItems();

                        // Calculate an xref that includes a "api-" prefix to avoid collisions
                        metadataItems.Add(WebKeys.Xref, $"api-{revision}" + doc.GetString(CodeAnalysisKeys.QualifiedName));

                        // Add the layout path if one was defined
                        NormalizedPath apiLayout = ctx.GetPath(DocsKeys.ApiLayout);
                        if (!apiLayout.IsNullOrEmpty)
                        {
                            metadataItems.Add(WebKeys.Layout, apiLayout);
                        }

                        // Change the content provider if needed
                        IContentProvider contentProvider = doc.ContentProvider;
                        if (ctx.GetBool(DocsKeys.OutputApiDocuments))
                        {
                            contentProvider = doc.ContentProvider.CloneWithMediaType(MediaTypes.Html);
                            metadataItems.Add(WebKeys.ContentType, ContentType.Content);
                        }

                        return doc.Clone(metadataItems, contentProvider);
                    }))
                )
            ).WithoutSourceMapping()
        );
    }
}
