namespace Documark.Cli.Parsing;

public class DocElement
{
    public string Name { get; set; } = null!;
    
    public ElementType Type { get; set; }

    public IEnumerable<DocElement> Children { get; set; } = [];

    public IEnumerable<DocNote> Notes { get; set; } = [];
}