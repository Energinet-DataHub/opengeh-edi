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
using System.Threading.Tasks;
using System.Xml.Linq;
using Messaging.Application.IncomingMessages.RequestChangeAccountPointCharacteristics;
using Messaging.CimMessageAdapter.Messages;
using Messaging.CimMessageAdapter.Messages.RequestChangeAccountingPointCharacteristics;
using Messaging.Domain.OutgoingMessages;
using Xunit;
using MarketActivityRecord = Messaging.Application.IncomingMessages.RequestChangeAccountPointCharacteristics.MarketActivityRecord;
using MessageHeader = Messaging.Application.IncomingMessages.MessageHeader;

namespace Messaging.Tests.CimMessageAdapter.Messages.RequestChangeAccountingPointCharacteristics;

public class MessageParserTests
{
    private readonly MessageParser _messageParser;

    public MessageParserTests()
    {
        _messageParser = new MessageParser(
            new IMessageParser<MarketActivityRecord, RequestChangeAccountingPointCharacteristicsTransaction>[]
            {
                new XmlMessageParser(),
            });
    }

    public static IEnumerable<object[]> CreateMessages()
    {
        return new List<object[]>
        {
            new object[] { CimFormat.Xml, CreateXmlMessage() },
        };
    }

    [Theory]
    [MemberData(nameof(CreateMessages))]
    public async Task Can_parse(CimFormat format, Stream message)
    {
        var result = await _messageParser.ParseAsync(message, format).ConfigureAwait(false);

        Assert.True(result.Success);
        AssertHeader(result.IncomingMarketDocument?.Header);
        var marketActivityRecord = result.IncomingMarketDocument?.MarketActivityRecords.First();
        Assert.Equal("25361487", marketActivityRecord?.Id);
        Assert.Equal("2022-12-17T23:00:00Z", marketActivityRecord?.EffectiveDate);
        Assert.Equal("579999993331812345", marketActivityRecord?.MarketEvaluationPoint.Id);
        Assert.Equal("E17", marketActivityRecord?.MarketEvaluationPoint.Type);
        Assert.Equal("E02", marketActivityRecord?.MarketEvaluationPoint.SettlementMethod);
        Assert.Equal("D01", marketActivityRecord?.MarketEvaluationPoint.MeteringMethod);
        Assert.Equal("D03", marketActivityRecord?.MarketEvaluationPoint.ConnectionState);
        Assert.Equal("PT1H", marketActivityRecord?.MarketEvaluationPoint.ReadCycle);
    }

    private static Stream CreateXmlMessage()
    {
        var xmlDoc = XDocument.Load($"cimmessageadapter{Path.DirectorySeparatorChar}messages{Path.DirectorySeparatorChar}xml{Path.DirectorySeparatorChar}RequestChangeAccountingPointCharacteristics.xml");
        var stream = new MemoryStream();
        xmlDoc.Save(stream);

        return stream;
    }

    private static void AssertHeader(MessageHeader? header)
    {
        Assert.Equal("253698245", header?.MessageId);
        Assert.Equal("E02", header?.ProcessType);
        Assert.Equal("5799999933318", header?.SenderId);
        Assert.Equal("DDM", header?.SenderRole);
        Assert.Equal("5790001330552", header?.ReceiverId);
        Assert.Equal("DDZ", header?.ReceiverRole);
        Assert.Equal("2022-12-17T09:30:47Z", header?.CreatedAt);
    }
}
