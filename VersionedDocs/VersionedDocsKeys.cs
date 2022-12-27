namespace Ociaw.VersionedDocs;

/// <summary>
/// Settings keys for VersionedDocs
/// </summary>
public static class VersionedDocsKeys
{
    /// <summary>
    /// A dictionary of version name to revision selector. For example, you could have a version named "Latest" with a
    /// revision selector of <c>master</c>, which populates Latest with code from the <c>master</c> branch. More advanced queries
    /// are possible depending on the VCS selected, such as using <c>tag(v0.3.0)</c> with mercurial to select the revision
    /// with the tag "v0.3.0". 
    /// </summary>
    /// <remarks>If no versions are specified, nothing will be generated.</remarks>
    public static String Versions => nameof(Versions);

    /// <summary>
    /// Path to the Git executable. Defaults to <c>git</c>.
    /// </summary>
    public static String GitExecutable => nameof(GitExecutable);

    /// <summary>
    /// Path to the Mercurial executable. Defaults to <c>hg</c>.
    /// </summary>
    public static String HgExecutable => nameof(HgExecutable);

    /// <summary>
    /// The repository type, either <see cref="VersionedDocs.RepositoryType.Git"/> or
    /// <see cref="VersionedDocs.RepositoryType.Hg"/>.
    /// </summary>
    public static String RepositoryType => nameof(RepositoryType);

    /// <summary>
    /// The path to the repository. Can be anything supported by the selected VCS's clone command.
    /// </summary>
    public static String RepositoryPath => nameof(RepositoryPath);
}
