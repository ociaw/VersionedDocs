using Statiq.CodeAnalysis;

namespace Ociaw.VersionedDocs;

/// <summary>
/// Hacky way to re-add metadata that's lost after running AnalyzeCSharp  
/// </summary>
public sealed class CopyMetadataAfterChildExecution : ParentModule
{
    private readonly Func<IExecutionContext, MetadataItems?> _metadataGetter;
    
    /// <summary>
    /// Creates a new instance of <see cref="CopyMetadataAfterChildExecution"/> with the specified child modules, copying
    /// the value of specified metadata key from the first input document to all the output documents.
    /// </summary>
    public CopyMetadataAfterChildExecution(String key, params IModule[] modules) : base(modules)
        => _metadataGetter = BuildFunc(new() { key });
    
    /// <summary>
    /// Creates a new instance of <see cref="CopyMetadataAfterChildExecution"/> with the specified child modules, copying
    /// the values of specified metadata keys from the first input document to all the output documents.
    /// </summary>
    public CopyMetadataAfterChildExecution(String key1, String key2, params IModule[] modules) : base(modules)
        => _metadataGetter = BuildFunc(new() { key1, key2 });
    
    /// <summary>
    /// Creates a new instance of <see cref="CopyMetadataAfterChildExecution"/> with the specified child modules, copying
    /// the values of specified metadata keys from the first input document to all the output documents.
    /// </summary>
    public CopyMetadataAfterChildExecution(String key1, String key2, String key3, params IModule[] modules) : base(modules)
        => _metadataGetter = BuildFunc(new() { key1, key2, key3 });

    /// <summary>
    /// Creates a new instance of <see cref="CopyMetadataAfterChildExecution"/> with the specified child modules, copying
    /// the values of specified metadata keys from the first input document to all the output documents.
    /// </summary>
    public CopyMetadataAfterChildExecution(IEnumerable<String> keys, params IModule[] modules) : base(modules)
        => _metadataGetter = BuildFunc(keys.ToList());

    /// <inheritdoc />
    protected override async Task<IEnumerable<IDocument>> ExecuteContextAsync(IExecutionContext context)
    {
        var metadata = _metadataGetter(context);
        var revDoc = context.Inputs.FirstOrDefault(i => i.GetString(CodeAnalysisKeys.Kind) == "Revision");

        var results = new List<IDocument>();
        foreach (var result in await context.ExecuteModulesAsync(Children, context.Inputs))
            results.Add(result.Clone(metadata));
        
        if (revDoc is not null)
            results.Add(revDoc);
        return results;
    }

    private static Func<IExecutionContext, MetadataItems?> BuildFunc(List<String> keys) => (context) =>
    {
        return context.Inputs.Where(i => i.ContainsKeys(keys)).Select(doc =>
        {
            var metadata = new MetadataItems();
            foreach (var key in keys)
                metadata.Add(key, doc.Get(key));
            return metadata;
        }).FirstOrDefault();
    };
}
