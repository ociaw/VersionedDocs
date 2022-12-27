using Statiq.CodeAnalysis;

namespace Ociaw.VersionedDocs;

/// <summary>
/// Hacky way to re-add metadata that's lost after running AnalyzeCSharp  
/// </summary>
public sealed class CopyMetadataAfterChildExecution : ParentModule
{
    private readonly String _key;
    
    /// <summary>
    /// Creates a new instance of <see cref="CopyMetadataAfterChildExecution"/> with the specified child modules.
    /// </summary>
    public CopyMetadataAfterChildExecution(String key, params IModule[] modules) : base(modules)
        => _key = key;

    /// <inheritdoc />
    protected override async Task<IEnumerable<IDocument>> ExecuteContextAsync(IExecutionContext context)
    {
        var value = context.Inputs.Select(i => i.Get(_key)).FirstOrDefault(i => i is not null);
        var metadata = new MetadataItems { { _key, value } };
        var revDoc = context.Inputs.FirstOrDefault(i => i.GetString(CodeAnalysisKeys.Kind) == "Revision");

        var results = new List<IDocument>();
        foreach (var result in await context.ExecuteModulesAsync(Children, context.Inputs))
            results.Add(result.Clone(metadata));
        
        if (revDoc is not null)
            results.Add(revDoc);
        return results;
    }
}
