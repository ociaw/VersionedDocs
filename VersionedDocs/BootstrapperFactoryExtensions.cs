namespace Ociaw.VersionedDocs;

/// <summary>
/// Extension methods on <see cref="BootstrapperFactory"/>.
/// </summary>
public static class BootstrapperFactoryExtensions
{
    /// <summary>
    /// Creates a bootstrapper with 
    /// </summary>
    /// <remarks>
    /// This includes Statiq Docs functionality so
    /// <see cref="Statiq.Docs.BootstrapperFactoryExtensions.CreateDocs(BootstrapperFactory, String[])"/>
    /// does not need to be called in addition to this method.
    /// </remarks>
    /// <param name="factory">The bootstrapper factory.</param>
    /// <param name="args">The command line arguments.</param>
    /// <returns>A bootstrapper.</returns>
    public static Bootstrapper CreateVersionedDocs(this BootstrapperFactory factory, String[] args) =>
        factory.CreateWeb(args).AddDocs().SupplementDocsWithVersioning();
}
