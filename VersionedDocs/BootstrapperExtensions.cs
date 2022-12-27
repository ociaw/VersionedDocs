namespace Ociaw.VersionedDocs;

/// <summary>
/// Extension methods on <see cref="IBootstrapper"/> implementations.
/// </summary>
public static class BootstrapperExtensions
{
    /// <summary>
    /// Supplements Statiq Docs functionality by modifying the Code and API pipelines and inserting new modules.
    /// </summary>
    /// <param name="boostrapper">The bootstrapper to supplement.</param>
    /// <returns>The bootstrapper.</returns>
    public static TBootstrapper SupplementDocsWithVersioning<TBootstrapper>(this TBootstrapper boostrapper)
        where TBootstrapper : IBootstrapper =>
        boostrapper
            .ConfigureEngine(engine =>
            {
                engine.Pipelines.Remove("VersionedApi");
                engine.Pipelines.Remove("Api");
                engine.Pipelines.Add("Api", new VersionedApi());
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
}
