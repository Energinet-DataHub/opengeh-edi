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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Messaging.CimMessageAdapter.Messages;
using Messaging.CimMessageAdapter.Messages.RequestChangeOfSupplier;
using Messaging.Domain.OutgoingMessages;
using Xunit;

namespace Messaging.Tests.CimMessageAdapter.Messages.RequestChangeOfSupplier;

public class MessageParserTests
{
    private readonly MessageParser _messageParser;

    public MessageParserTests()
    {
        _messageParser = new MessageParser(
            new IMessageParser[]
            {
                new JsonMessageParser(),
                new XmlMessageParser(),
            });
    }

    public static IEnumerable<object[]> CreateMessages()
    {
        return new List<object[]>
        {
            new object[] { CimFormat.Xml, CreateXmlMessage() },
            new object[] { CimFormat.Json, CreateJsonMessage() },
        };
    }

    [Theory]
    [MemberData(nameof(CreateMessages))]
    public async Task Can_parse(CimFormat format, Stream message)
    {
        var result = await _messageParser.ParseAsync(message, format).ConfigureAwait(false);

        Assert.True(result.Success);
        Assert.Equal("78954612", result.MessageHeader?.MessageId);
        Assert.Equal("E65", result.MessageHeader?.ProcessType);
        Assert.Equal("5799999933318", result.MessageHeader?.SenderId);
        Assert.Equal("DDQ", result.MessageHeader?.SenderRole);
        Assert.Equal("5790001330552", result.MessageHeader?.ReceiverId);
        Assert.Equal("DDZ", result.MessageHeader?.ReceiverRole);
        Assert.Equal("2022-09-07T09:30:47Z", result.MessageHeader?.CreatedAt);
        var marketActivityRecord = result.MarketActivityRecords.First();
        Assert.Equal("12345689", marketActivityRecord.Id);
        Assert.Equal("579999993331812345", marketActivityRecord.MarketEvaluationPointId);
        Assert.Equal("5799999933318", marketActivityRecord.EnergySupplierId);
        Assert.Equal("5799999933340", marketActivityRecord.BalanceResponsibleId);
        Assert.Equal("0801741527", marketActivityRecord.ConsumerId);
        Assert.Equal("ARR", marketActivityRecord.ConsumerIdType);
        Assert.Equal("Jan Hansen", marketActivityRecord.ConsumerName);
        Assert.Equal("2022-09-07T22:00:00Z", marketActivityRecord.EffectiveDate);
    }

    private static Stream CreateXmlMessage()
    {
        var xmlDoc = XDocument.Load($"cimmessageadapter{Path.DirectorySeparatorChar}messages{Path.DirectorySeparatorChar}xml{Path.DirectorySeparatorChar}RequestChangeOfSupplier.xml");
        var stream = new MemoryStream();
        xmlDoc.Save(stream);

        return stream;
    }

    private static MemoryStream CreateJsonMessage()
    {
        return ReadTextFile(
            $"cimmessageadapter{Path.DirectorySeparatorChar}messages{Path.DirectorySeparatorChar}json{Path.DirectorySeparatorChar}Request Change of Supplier.json");
    }

    private static MemoryStream ReadTextFile(string path)
    {
        var jsonDoc = File.ReadAllText(path);
        var stream = new MemoryStream();
        using var writer = new StreamWriter(stream: stream, encoding: Encoding.UTF8, bufferSize: 4096, leaveOpen: true);
        writer.Write(jsonDoc);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }
}
