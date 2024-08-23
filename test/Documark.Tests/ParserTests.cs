using Documark.Cli.Parsing;

namespace Documark.Tests;

[TestClass]
public class ParserTests
{
    [TestMethod]
    public void Can_Parse_File()
    {
        var parser = Parser.FromFile(
            Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, 
                "Documark.Cli.xml"
            )
        );
        
        var result = parser.Parse();
        
        Assert.IsNotNull(result);
        Assert.IsTrue(!string.IsNullOrWhiteSpace(result.Name));
    }
}