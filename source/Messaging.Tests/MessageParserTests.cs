using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Xml.Linq;
using Messaging.CimMessageAdapter.Messages;
using Xunit;

namespace Messaging.Tests;

#pragma warning disable
public class MessageParserTests
{
    [Fact]
    public async Task Message_parser_can_parse_xml_messages()
    {
        var parser = new XmlMessageParser(MediaTypeNames.Application.Xml);
        var stream = LoadXmlFileAsMemoryStream();
        var parsedResult = await parser.ParseAsync(stream).ConfigureAwait(false);

        Assert.True(parsedResult.Success);
    }

    [Fact]
    public async Task Message_parser_can_parse_json_messages()
    {
        var parser = new JsonMessageParser(MediaTypeNames.Application.Json);
        var stream = LoadJsonFileAsMemoryStream();
        var parsedResult = await parser.ParseAsync(stream).ConfigureAwait(false);

        Assert.True(parsedResult.Success);
    }

    private static Stream LoadXmlFileAsMemoryStream()
    {
        var xmlDoc = XDocument.Load($"xml{Path.DirectorySeparatorChar}Confirm request Change of Supplier.xml");
        var stream = new MemoryStream();
        xmlDoc.Save(stream);

        return stream;
    }

    private static Stream LoadJsonFileAsMemoryStream()
    {
        var jsonDoc = File.ReadAllText($"json{Path.DirectorySeparatorChar}Request Change of Supplier.json");
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(jsonDoc);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    #pragma warning restore
}
