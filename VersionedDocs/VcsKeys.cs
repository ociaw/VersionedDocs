namespace Ociaw.VersionedDocs;

/// <summary>
/// Metadata keys for the <see cref="Vcs"/> pipeline.
/// </summary>
public static class VcsKeys
{
    /// <summary>
    /// Path to the on-disk repository.
    /// </summary>
    public static String RepositoryRoot => nameof(RepositoryRoot);

    /// <summary>
    /// The name of a revision.
    /// </summary>
    public static String RevisionName => nameof(RevisionName);

    /// <summary>
    /// The string used to select the repository revision. 
    /// </summary>
    public static String RevisionSelector => nameof(RevisionSelector);

    /// <summary>
    /// The ID of a revision, aka the commit hash.
    /// </summary>
    public static String RevisionId => nameof(RevisionId);

    /// <summary>
    /// The timestamp of the commit date.
    /// </summary>
    public static String RevisionTimestamp => nameof(RevisionTimestamp);
}
