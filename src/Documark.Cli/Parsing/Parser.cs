using System.Xml;

namespace Documark.Cli.Parsing;

/// <summary>
/// Parses XML documentation files into elements
/// </summary>
public sealed class Parser
{
    public static Parser FromStream(Stream stream)
    {
        var streamReader = new StreamReader(stream);
        return new Parser(streamReader.ReadToEnd());
    }

    public static Parser FromFile(string filePath)
    {
        var stream = File.OpenRead(filePath);
        return FromStream(stream);
    }

    private readonly XmlDocument _document;

    public Parser(string xml)
    {
        _document = new XmlDocument();
        _document.PreserveWhitespace = false;
        _document.LoadXml(xml);
    }

    /// <summary>
    /// Parses the xml
    /// </summary>
    /// <returns>A DocAssembly object representing the loaded and parsed XML file</returns>
    public DocAssembly Parse()
    {
        DocAssembly assembly = new()
        {
            Name = _document.DocumentElement?.SelectSingleNode("assembly")?.InnerText ?? string.Empty,
        };
        
        Console.WriteLine($"Parsing assembly: {assembly.Name}");
        
        var namespaceElements = new List<DocElement>();
        
        var memberNodes = _document.DocumentElement?.SelectSingleNode("members")?.ChildNodes;
        if (memberNodes is null)
        {
            Console.WriteLine("No member nodes!");
            return assembly;
        }
        
        Console.WriteLine($"Found members: {memberNodes.Count}");

        Func<string, DocElement> getNamespaceElement = (__namespace) =>
        {
            var result = namespaceElements.FirstOrDefault(x => x.Name == __namespace);

            if (result == null)
            {
                result = new DocElement
                {
                    Name = __namespace,
                    Type = ElementType.Namespace
                };

                namespaceElements.Add(result);
            }

            return result;
        };
        
        foreach (XmlNode memberNode in memberNodes)
        {
            var nameData = memberNode.Attributes?.GetNamedItem("name")?.InnerText;
    
            if (string.IsNullOrEmpty(nameData))
            {
                Console.WriteLine("No name data...");
                continue;
            }

            // grab the type data and remove it
            var typeData = nameData[0];
            nameData = nameData.Substring(2);
            
            var curElement = new DocElement();
            
            // set the element type and do any necessary cutting of the namespace
            switch (typeData)
            {
                case 'T':
                    curElement.Type = ElementType.Type;
                    break;
                case 'M':
                    curElement.Type = ElementType.Method;
                    
                    // remove the parameters for now
                    // TODO: Add support for doing something with the parameters
                    var indexOfParenthesis = nameData.IndexOf("(");
                    nameData = nameData.Substring(0,  indexOfParenthesis == -1 ? nameData.Length : indexOfParenthesis);
                    break;
                case 'P':
                    curElement.Type = ElementType.Property;
                    break;
            }
            
            // get the namespace and name
            var namespaceData = nameData.Split(["."], StringSplitOptions.RemoveEmptyEntries);
            
            // grab the namespace
            var curNamespace = string.Join(
                ".", 
                namespaceData.Take(namespaceData.Length - (curElement.Type == ElementType.Type ? 1 : 2)) 
            );

            // set the name
            curElement.Name = namespaceData.Last();
            
            // get all the children and convert them
            var noteNodes = memberNode.ChildNodes;
            foreach (XmlNode noteNode in noteNodes)
            {
                var note = new DocNote();

                switch (noteNode.Name.ToLower())
                {
                    case "summary":
                        note.Type = NoteType.Summary;
                        note.Name = "Summary";
                        note.Content = noteNode.InnerText.Trim();
                        break;
                    case "param":
                        note.Type = NoteType.Param;
                        note.Name = noteNode.Attributes?.GetNamedItem("name")?.InnerText ?? "Unknown Parameter";
                        note.Content = noteNode.InnerText.Trim();
                        break;
                    case "example":
                        note.Type = NoteType.Example;
                        note.Name = "Example";
                        note.Content = noteNode.InnerText;
                        break;
                    case "returns":
                        note.Type = NoteType.Returns;
                        note.Name = "Returns";
                        note.Content = noteNode.InnerText;
                        break;
                    case "exception":
                        note.Type = NoteType.Exception;
                        note.Name = "Exception";
                        note.Content = noteNode.InnerText;
                        break;
                    default:
                        note.Type = NoteType.Unknown;
                        note.Name = noteNode.Name;
                        note.Content = noteNode.InnerText;
                        break;
                }

                curElement.Notes = [..curElement.Notes, note];
            }

            // get a reference to the namespace and add the element
            var ns = getNamespaceElement(curNamespace);

            if (curElement.Type == ElementType.Type)
            {
                ns.Children = [..ns.Children, curElement];
            }
            else
            {
                var typeName = namespaceData[^2];
                
                Console.WriteLine($"Adding {curElement.Type.ToString().ToLower()}, {curElement.Name}, to type {typeName}");
                
                var typeElement = ns.Children.FirstOrDefault(x => x.Name == typeName);

                if (typeElement is null)
                {
                    Console.WriteLine($"Cannot find type: {typeName}");
                    continue;
                }
                
                typeElement.Children = [..typeElement.Children, curElement];
            }
        }
        
        // set the namespaces
        assembly.Namespaces = namespaceElements.ToArray();

        return assembly;
    }
}