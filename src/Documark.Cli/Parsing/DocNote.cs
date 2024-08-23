namespace Documark.Cli.Parsing;

public class DocNote
{
    public string Name { get; set; } = null!;

    public string Content { get; set; } = null!;

    public NoteType Type { get; set; } = NoteType.Unknown;
}