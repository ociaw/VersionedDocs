namespace Ociaw.VersionedDocs;

/// <summary>
/// Executes the Child modules of this module against the Children of the input documents.
/// </summary>
public sealed class ExecuteOverChildDocuments : ParentModule
{
    private readonly String _children = Keys.Children;

    /// <summary>
    /// Creates a new instance of <see cref="ExecuteOverChildDocuments"/> with the specified child modules.
    /// </summary>
    public ExecuteOverChildDocuments(params IModule[] modules) : base(modules)
    { }

    /// <inheritdoc />
    protected override async Task<IEnumerable<IDocument>> ExecuteInputAsync(IDocument input, IExecutionContext context) =>
        await context.ExecuteModulesAsync(Children, input.GetChildren(_children));
}
