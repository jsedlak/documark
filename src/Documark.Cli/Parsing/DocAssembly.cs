namespace Documark.Cli.Parsing;

/// <summary>
/// Assembly level documentation element
/// </summary>
public class DocAssembly
{
    /// <summary>
    /// The name of the assembly
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// A collection of elements representing the namespaces in the assembly
    /// </summary>
    public IEnumerable<DocElement> Namespaces { get; set; } = [];
}