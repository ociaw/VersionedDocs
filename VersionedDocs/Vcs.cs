using System.Globalization;
using Microsoft.Extensions.Logging;
using Statiq.CodeAnalysis;

namespace Ociaw.VersionedDocs;

/// <summary>
/// A pipeline that clones a Git or Mercurial repository and sets up folders for each version.
/// </summary>
public sealed class Vcs : Pipeline
{
    /// <summary>
    /// Creates a new instance of <see cref="Vcs"/>.
    /// </summary>
    public Vcs()
    {
        DependencyOf.AddRange("Api", "Code");
        InputModules = new ModuleList
        {
            new ExecuteConfig(Config.FromContext<IEnumerable<IDocument>>(context =>
            {
                var versions = context.Get<Dictionary<String, Object>>(VersionedDocsKeys.Versions);
                if (versions is null)
                {
                    context.LogWarning("No versions defined, skipping.");
                    return Enumerable.Empty<IDocument>();
                }

                context.LogDebug("{count} versions found", versions.Count);
                return context.Get<Dictionary<String, Object>>(VersionedDocsKeys.Versions).Select(kvp =>
                {
                    // TODO: How to inject this?
                    var fileSystem = new FileSystem();
                    var revision = kvp.Key;
                    var selector = kvp.Value;
                    var metadata = new MetadataItems();
                    metadata.Add(Keys.Title, revision);
                    metadata.Add(VcsKeys.RevisionName, revision);
                    metadata.Add(VcsKeys.RevisionSelector, selector);

                    var physicalDir = fileSystem.GetTempDirectory($"docs-{revision}-{Path.GetRandomFileName()}").Path;
                    metadata.Add(VcsKeys.RepositoryRoot, physicalDir);

                    // Set CodeAnalysis stuff - TODO: This should probably be moved into VersionedApi, or its own pipeline 
                    metadata.Add(CodeAnalysisKeys.Kind, "Revision");
                    metadata.Add(CodeAnalysisKeys.SpecificKind, "Revision");
                    metadata.Add(CodeAnalysisKeys.DisplayName, revision);

                    var source = physicalDir;
                    var destination = NormalizedPath.Combine(context.GetString(DocsKeys.ApiPath), kvp.Key, context.Settings.GetIndexFileName());
                    return context.CreateDocument(source, destination, metadata, new NullContent());
                });
            })),
            new ExecuteSwitch(Config.FromSetting(VersionedDocsKeys.RepositoryType, RepositoryType.Git)).Case(RepositoryType.Hg,
                new StartProcess(Config.FromSetting(VersionedDocsKeys.HgExecutable, "hg"))
                    .WithArgument("clone")
                    .WithArgument(Config.FromSetting(VersionedDocsKeys.RepositoryPath))
                    .WithArgument(Config.FromDocument(doc => doc.GetString(VcsKeys.RepositoryRoot))),
                new StartProcess(Config.FromSetting(VersionedDocsKeys.HgExecutable, "hg"))
                    .WithWorkingDirectory(Config.FromDocument(doc => doc.GetString(VcsKeys.RepositoryRoot)))
                    .WithArgument("update")
                    .WithArgument("--rev")
                    .WithArgument(Config.FromDocument(doc => doc.GetString(VcsKeys.RevisionSelector))),
                new StartProcess(Config.FromSetting(VersionedDocsKeys.HgExecutable, "hg"))
                    .WithWorkingDirectory(Config.FromDocument(doc => doc.GetString(VcsKeys.RepositoryRoot)))
                    .WithArgument("id")
                    .WithArgument("--debug")
                    .WithArgument("--template")
                    .WithArgument("{node} {date}", true)
                    .WithArgument("--rev")
                    .WithArgument(Config.FromDocument(doc => doc.GetString(VcsKeys.RevisionSelector)))
            ).Case(RepositoryType.Git,
                new StartProcess(Config.FromSetting(VersionedDocsKeys.GitExecutable, "git"))
                    .WithArgument("-c")
                    .WithArgument("advice.detachedHead=false")
                    .WithArgument("clone")
                    .WithArgument("--quiet")
                    .WithArgument("--depth")
                    .WithArgument("1")
                    .WithArgument("--branch")
                    .WithArgument(Config.FromDocument(doc => doc.GetString(VcsKeys.RevisionSelector)))
                    .WithArgument(Config.FromSetting(VersionedDocsKeys.RepositoryPath))
                    .WithArgument(Config.FromDocument(doc => doc.GetString(VcsKeys.RepositoryRoot))),
                new StartProcess(Config.FromSetting(VersionedDocsKeys.GitExecutable, "git"))
                    .WithWorkingDirectory(Config.FromDocument(doc => doc.GetString(VcsKeys.RepositoryRoot)))
                    .WithArgument("log")
                    .WithArgument("-1")
                    .WithArgument("--format=%H %ct", true)
                    .WithArgument(Config.FromDocument(doc => doc.GetString(VcsKeys.RevisionSelector)))
            ),
            new SetMetadata(VcsKeys.RevisionId,
                Config.FromDocument(async doc => (await doc.GetContentStringAsync()).Split(' ')[0])
            ),
            new SetMetadata(VcsKeys.RevisionTimestamp, Config.FromDocument(async doc =>
            {
                var str = (await doc.GetContentStringAsync()).Split(' ')[1];
                var epochSeconds = Decimal.Parse(str, CultureInfo.InvariantCulture);
                var epochMillis = epochSeconds * 1000;
                return DateTimeOffset.FromUnixTimeMilliseconds((Int64)epochMillis);
            })),
            new SetContent(new NullContent())
        };
    }
}
