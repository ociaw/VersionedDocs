using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Statiq.CodeAnalysis;

namespace Ociaw.VersionedDocs;

/// <summary>
/// Extension methods on <see cref="IBootstrapper"/> implementations.
/// </summary>
public static class BootstrapperExtensions
{
    /// <summary>
    /// Supplements Statiq Docs functionality by modifying the Code and API pipelines and inserting new modules.
    /// </summary>
    /// <param name="bootstrapper">The bootstrapper to supplement.</param>
    /// <returns>The bootstrapper.</returns>
    public static TBootstrapper SupplementDocsWithVersioning<TBootstrapper>(this TBootstrapper bootstrapper)
        where TBootstrapper : IBootstrapper =>
        bootstrapper
            .ConfigureEngine(engine =>
            {
                ModifyApi(engine.Pipelines["Api"]);
                ModifyCode(engine.Pipelines["Code"]);
            })
            .AddPipeline(typeof(Vcs));

    // Replace the guts of Code pipeline so that it reads from directories produced by the VCS pipeline 
    private static void ModifyCode(IPipeline pipeline)
    {
        pipeline.InputModules.Clear();
        pipeline.ProcessModules.Add(
            new ConcatDocuments(nameof(Vcs)),
            new ReadFiles(Config.FromDocument((doc, ctx) =>
            {
                var root = doc.GetPath(VcsKeys.RepositoryRoot);
                return ctx.GetList<String>(DocsKeys.SourceFiles).Select(glob => root.Combine(glob).FullPath);
            }))
        );
    }

    private static void ModifyApi(IPipeline pipeline)
    {
        pipeline.Dependencies.Add(nameof(Vcs));

        // Drill down to the if condition
        if (pipeline.ProcessModules[0] is not ExecuteIf rootIf)
            throw new InvalidOperationException($"Expected root process module to be ExecuteIf, got {pipeline.ProcessModules[0].GetType()}");

        var rootCondition = rootIf[0];
        // First we concat VCS outputs, then group all documents by the revision name
        rootCondition.InsertAfterFirst<ConcatDocuments>(
            new ConcatDocuments(nameof(Vcs)),
            new GroupDocuments(VcsKeys.RevisionName)
        );
        var cache = rootCondition.GetFirst<CacheDocuments>();

        // Replace the Analyze C# config with a custom one
        // Insert a new module handle revision-specific documents and metadata links.
        // This results in three Children: 
        var children = cache.Children;
        children.ReplaceFirst<ExecuteConfig>(BuildNewAnalyzeModule());
        children.InsertAfterLast<ExecuteConfig>(BuildRevisionLinkModule());

        // Now move the 3 child modules into a new ExecuteOverChildDocuments module
        // This becomes the one and only child of the cache
        var innerCached = new ExecuteOverChildDocuments(cache.Children.ToArray());
        cache.Children.Clear();
        cache.Children.Add(innerCached);
    }

    /// <summary>
    /// This module supports Versioning by re-adding revision metadata like RevisionName, RevisionID, and
    /// RevisionTimestamp. It also adds the revision name to the destination prefix. 
    /// </summary>
    private static IModule BuildNewAnalyzeModule() => new CopyMetadataAfterChildExecution
    (
        VcsKeys.RevisionName, VcsKeys.RevisionId, VcsKeys.RevisionTimestamp, new ExecuteConfig(Config.FromContext(ctx =>
            new AnalyzeCSharp()
                .WhereNamespaces(ctx.Settings.GetBool(DocsKeys.IncludeGlobalNamespace))
                .WherePublic()
                .WithCssClasses("code", "language-csharp")
                .WithDestinationPrefix(ctx.GetPath(DocsKeys.ApiPath).Combine(ctx.Inputs.First().GetString(VcsKeys.RevisionName)))
                .WithAssemblies(Config.FromContext<IEnumerable<String>>(c => c.GetList<String>(DocsKeys.AssemblyFiles)))
                .WithProjects(Config.FromContext<IEnumerable<String>>(c => c.GetList<String>(DocsKeys.ProjectFiles)))
                .WithSolutions(Config.FromContext<IEnumerable<String>>(c => c.GetList<String>(DocsKeys.SolutionFiles)))
                .WithAssemblySymbols()
                .WithImplicitInheritDoc(ctx.GetBool(DocsKeys.ImplicitInheritDoc))
        ))
    );

    private static IModule BuildRevisionLinkModule() => new ExecuteConfig(Config.FromDocument((doc, ctx) =>
    {
        if (doc.GetString(CodeAnalysisKeys.Kind) is "Revision")
        {
            String name = doc.GetString(VcsKeys.RevisionName);
            TypeNameLinks typeNameLinks = ctx.GetRequiredService<TypeNameLinks>();
            typeNameLinks.Links.AddOrUpdate(WebUtility.HtmlEncode(name), ctx.GetLink(doc), (_, _) => String.Empty);
        }

        String revision = doc.GetString(VcsKeys.RevisionName);
        // Ensure Xref contains the revision name
        MetadataItems metadataItems = new MetadataItems();
        metadataItems.Add(WebKeys.Xref, doc.GetString(WebKeys.Xref).Replace("api-", $"api-{revision}-"));

        return doc.Clone(metadataItems);
    }));
}
