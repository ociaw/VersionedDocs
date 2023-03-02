# VersionedDocs

[![VersionedDocs on NuGet](https://img.shields.io/nuget/v/VersionedDocs)](https://www.nuget.org/packages/Ociaw.VersionedDocs/)
[![Latest VersionedDocs release on GitHub](https://img.shields.io/github/v/tag/ociaw/VersionedDocs)](https://github.com/ociaw/VersionedDocs/releases/)

VersionDocs builds on top of [Statiq Docs](https://www.statiq.dev/docs) to add support for versioned APIs. This allows documentation for
multiple API versions to exist on the same site at once. For example, two separate library versions can be documented independently at
`/api/v1.0` and `/api/v2.0`.

## Usage

Instead of calling `.CreateDocs()`, call `.CreateVersionedDocs()`:

```csharp
using using Ociaw.VersionedDocs;

await Bootstrapper.Factory
    .CreateVersionedDocs(args)
    .RunAsync();
```

You'll also need a theme supporting versioning, such as [Verdoct](https://github.com/ociaw/Verdoct) or a
[modified Docable](https://github.com/ociaw/Docable/tree/versioned-api).

In your settings.yml file you'll need to define several settings - RepositoryType, RepositoryPath, and Versions.

### Git Example:
```yaml
RepositoryType: Git # Optional since git is the default
RepositoryPath: https://github.com/ociaw/RandN.git
Versions:
  master: master
  v0.3.0: v0.3.0
 ```

### Mercurial Example:
```yaml
RepositoryType: Hg
RepositoryPath: https://hg.sr.ht/~ociaw/RandN
Versions:
  master: bookmark(master)
  v0.3.0: tag(v0.3.0)
 ```

## Settings
- Versions - a map of RevisionNames to RevisionSelectors
- RepositoryType: `string` - the type of repository used, can either be `Hg` or `Git`; defaults to "Git"
- RepositoryPath: `string` - the path of the repository to clone
- GitExecutable: `string` - the path to the git executable; defaults to "git"
- HgExecutable: `string` - the path to the hg executable; defaults to "hg"

### Revision Selectors
Revision selector syntax varies by VCS type. For mercurial, revset syntax can be used. Git accepts the name of branches and tags.

## Document Keys
These keys are added to API symbol documents and can be used in themes.

- RevisionName: `string` - The name of the revision that the document is from
- RevisionId: `string` - The ID, or commit hash, of the selected revision
- RevisionTimestamp: `DateTimeOffset` - The timestamp at which the selected revision was committed
