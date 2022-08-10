// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Messaging.CimMessageAdapter.Messages;
using Messaging.Domain.SeedWork;
using Messaging.IntegrationTests;
using Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace Messaging.Tests;

public class MessageParserTests : TestBase
{
    private readonly JsonMessageParserStrategy _jsonMessageParserStrategy;

    public MessageParserTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _jsonMessageParserStrategy = GetService<JsonMessageParserStrategy>();
    }

    #pragma warning disable
    [Theory]
    [MemberData(nameof(LoadValidMessages))]
    public async Task Can_parse_message(CimFormat format, Stream message)
    {
        var parser = new MessageParser(new XmlMessageParserStrategy(), new JsonMessageParserStrategy());
        var parsedResult = await parser.GetMessageParserStrategy(format).ParseAsync(message).ConfigureAwait(false);

        Assert.True(parsedResult.Success);
    }

    public static IEnumerable<object[]> LoadValidMessages()
    {
        return new List<object[]>
        {
            new object[] { CimFormat.Xml, LoadXmlFileAsMemoryStream() },
            new object[] { CimFormat.Json, LoadJsonFileAsMemoryStream() },
        };
    }

    [Fact]
    public async Task Message_parser_returns_error_when_json_is_invalid()
    {
        using var stream = LoadInvalidJsonFileAsMemoryStream();
        var parsedResult = await _jsonMessageParserStrategy.ParseAsync(stream).ConfigureAwait(false);

        Assert.False(parsedResult.Success);
        Assert.Equal(2, parsedResult.Errors.Count);
    }

    private static Stream LoadXmlFileAsMemoryStream()
    {
        var xmlDoc = XDocument.Load($"cimmessageadapter{Path.DirectorySeparatorChar}messages{Path.DirectorySeparatorChar}xml{Path.DirectorySeparatorChar}Confirm request Change of Supplier.xml");
        var stream = new MemoryStream();
        xmlDoc.Save(stream);

        return stream;
    }

    private static MemoryStream LoadJsonFileAsMemoryStream()
    {
        var jsonDoc = File.ReadAllText($"cimmessageadapter{Path.DirectorySeparatorChar}messages{Path.DirectorySeparatorChar}json{Path.DirectorySeparatorChar}Request Change of Supplier.json");
        var stream = new MemoryStream();
        using var writer = new StreamWriter(stream: stream, encoding: Encoding.UTF8, bufferSize: 4096, leaveOpen: true);
        writer.Write(jsonDoc);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    private static MemoryStream LoadInvalidJsonFileAsMemoryStream()
    {
        var jsonDoc = File.ReadAllText($"cimmessageadapter{Path.DirectorySeparatorChar}messages{Path.DirectorySeparatorChar}json{Path.DirectorySeparatorChar}Invalid Request Change of Supplier.json");
        var stream = new MemoryStream();
        using var writer = new StreamWriter(stream: stream, encoding: Encoding.UTF8, bufferSize: 4096, leaveOpen: true);
        writer.Write(jsonDoc);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }
}
