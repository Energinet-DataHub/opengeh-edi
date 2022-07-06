using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using Messaging.CimMessageAdapter.Messages;
using Xunit;

namespace Messaging.Tests;

public class MessageParserTests
{
    private readonly XmlMessageParser _xmlMessageParser;
    private readonly JsonMessageParser _jsonMessageParser;

    public MessageParserTests()
    {
        _xmlMessageParser = new XmlMessageParser();
        _jsonMessageParser = new JsonMessageParser();
    }

    #pragma warning disable
    [Fact]
    public async Task Message_parser_can_parse_xml_messages()
    {
        using var stream = LoadXmlFileAsMemoryStream();
        var parsedResult = await _xmlMessageParser.ParseAsync(stream).ConfigureAwait(false);

        Assert.True(parsedResult.Success);
    }

    [Fact]
    public async Task Message_parser_can_parse_json_messages()
    {
        var stream = LoadJsonFileAsMemoryStream();
        var parsedResult = await _jsonMessageParser.ParseAsync(stream).ConfigureAwait(false);

        Assert.True(parsedResult.Success);
    }

    [Fact]
    public async Task Message_parser_returns_error_when_json_is_invalid()
    {
        var stream = LoadInvalidJsonFileAsMemoryStream();
        var parsedResult = await _jsonMessageParser.ParseAsync(stream).ConfigureAwait(false);

        Assert.False(parsedResult.Success);
        Assert.Equal(2, parsedResult.Errors.Count);
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

    private static Stream LoadInvalidJsonFileAsMemoryStream()
    {
        var jsonDoc = File.ReadAllText($"json{Path.DirectorySeparatorChar}Invalid Request Change of Supplier.json");
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(jsonDoc);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }
    #pragma warning restore
}
