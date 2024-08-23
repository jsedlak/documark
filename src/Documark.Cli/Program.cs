// See https://aka.ms/new-console-template for more information

using System.ComponentModel;
using System.Text;
using Documark.Cli.Parsing;
using Spectre.Console.Cli;

var app = new CommandApp<ConvertXmlCommand>();
app.Run(args);

public sealed class ConvertXmlCommand : Command<ConvertXmlCommandSettings>
{
    public override int Execute(CommandContext context, ConvertXmlCommandSettings settings)
    {
        if (Directory.Exists(settings.Output))
        {
            Directory.Delete(settings.Output, true);
        }
        
        Console.WriteLine($"Parsing {settings.FilePath}");
        Console.WriteLine($"Output Directory: {new DirectoryInfo(settings.Output).FullName}");
        
        var parser = Parser.FromFile(settings.FilePath);
        var result = parser.Parse();
        
        Console.WriteLine($"Namespaces Found: {result.Namespaces.Count()}");

        foreach (var ns in result.Namespaces)
        {
            var splitNamespaceName = ns.Name.Split(["."], StringSplitOptions.RemoveEmptyEntries);
            var nsDirectory = EnsureDirectories(settings.Output, splitNamespaceName);

            Console.WriteLine($"Types Found: {ns.Children.Count()}");
            
            foreach (var typeElement in ns.Children)
            {
                var file = Path.Combine(nsDirectory, typeElement.Name + ".md");
                
                Console.WriteLine($"Writing {file}");
                
                var md = BuildMarkdown(typeElement);
                File.WriteAllText(
                    file,
                    md
                );
            }
        }

        return 0;
    }

    private string BuildMarkdown(DocElement element)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# {element.Name}");
        sb.AppendLine();

        var children = element.Children.GroupBy(m => m.Type);

        foreach (var group in children)
        {
            switch (group.Key)
            {
                case ElementType.Method:
                    sb.AppendLine($"## Methods");
                    sb.AppendLine("| Name | Summary |");
                    sb.AppendLine("| ---- | -------- |");
                    foreach (var method in group)
                    {
                        var summary = method.Notes.FirstOrDefault(m => m.Type == NoteType.Summary)?.Content ?? "";
                        sb.AppendLine($"| {method.Name} | {summary} |");
                    }
                    break;
                case ElementType.Property:
                    sb.AppendLine($"## Properties");
                    sb.AppendLine("| Name | Summary |");
                    sb.AppendLine("| ---- | -------- |");
                    foreach (var property in group)
                    {
                        var summary = property.Notes.FirstOrDefault(m => m.Type == NoteType.Summary)?.Content ?? "";
                        sb.AppendLine($"| {property.Name} | {summary} |");
                    }
                    break;
            }
        }
        
        WriteNotes(sb, element);
        
        return sb.ToString();
    }

    private void WriteNotes(StringBuilder stringBuilder, DocElement element)
    {
        var notesByType = element.Notes.GroupBy(n => n.Type);

        foreach (var noteGrouping in notesByType)
        {
            switch (noteGrouping.Key)
            {
                case NoteType.Summary:
                case NoteType.Returns:
                    stringBuilder.AppendLine($"## {noteGrouping.Key}");
                    foreach (var childNote in noteGrouping)
                    {
                        stringBuilder.AppendLine(childNote.Content);
                    }

                    break;
                case NoteType.Unknown:
                    foreach (var childNote in noteGrouping)
                    {
                        stringBuilder.AppendLine($"## {childNote.Name}");
                        stringBuilder.AppendLine(childNote.Content);
                    }
                    break;
                case NoteType.Param:
                    stringBuilder.AppendLine($"## Parameters");
                    stringBuilder.AppendLine("| Parameter |");
                    stringBuilder.AppendLine("| --------- |");
                    foreach (var childNote in noteGrouping)
                    {
                        stringBuilder.AppendLine($"| {childNote.Content} |");
                    }
                    break;
                case NoteType.Exception:
                    stringBuilder.AppendLine($"## Exceptions");
                    stringBuilder.AppendLine("| Exception |");
                    stringBuilder.AppendLine("| --------- |");
                    foreach (var childNote in noteGrouping)
                    {
                        stringBuilder.AppendLine($"| {childNote.Content} |");
                    }
                    break;
                case NoteType.Example:
                    stringBuilder.AppendLine($"## Examples");
                    foreach (var childNote in noteGrouping)
                    {
                        stringBuilder.AppendLine($"```");
                        stringBuilder.AppendLine(childNote.Content);
                        stringBuilder.AppendLine("```");
                        stringBuilder.AppendLine();
                    }
                    break;
            }

            
        }
    }

    private string EnsureDirectories(string baseDirectory, IReadOnlyList<string> directories)
    {
        var cur = baseDirectory;

        if (!Directory.Exists(cur))
        {
            Directory.CreateDirectory(cur);
        }

        foreach (var dir in directories)
        {
            cur = Path.Combine(cur, dir);
            
            if (!Directory.Exists(cur))
            {
                Directory.CreateDirectory(cur);
            }
        }

        return cur;
    }
}

public sealed class ConvertXmlCommandSettings : CommandSettings
{
    [Description(".NET XML File to convert to Markdown")]
    [CommandArgument(0, "[filePath]")]
    public string FilePath { get; init; } = null!;

    [Description("How documentation is organized by namespace: tree or flat")]
    [CommandOption("-f|--format")]
    [DefaultValue("tree")]
    public string Format { get; init; } = null!;

    [Description("Where the output files should go.")]
    [CommandOption("-o|--output")]
    public string Output { get; set; } = null!;
}