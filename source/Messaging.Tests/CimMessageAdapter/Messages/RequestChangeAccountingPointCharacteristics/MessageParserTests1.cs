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

using System;
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

namespace Messaging.Tests.CimMessageAdapter.Messages.RequestChangeAccountingPointCharacteristics;

#pragma warning disable
public class MessageParserTests1
{
    private readonly MessageParser _messageParser;

    public MessageParserTests1()
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
    }

    private static Stream CreateXmlMessage()
    {
        var xmlDoc = XDocument.Load($"cimmessageadapter{Path.DirectorySeparatorChar}messages{Path.DirectorySeparatorChar}xml{Path.DirectorySeparatorChar}RequestChangeAccountingPointCharacteristics.xml");
        var stream = new MemoryStream();
        xmlDoc.Save(stream);

        return stream;
    }
}

public class MessageParser
{
    private readonly IEnumerable<IMessageParser<MarketActivityRecord, RequestChangeAccountingPointCharacteristicsTransaction>> _parsers;

    public MessageParser(IEnumerable<IMessageParser<MarketActivityRecord, RequestChangeAccountingPointCharacteristicsTransaction>> parsers)
    {
        _parsers = parsers;
    }

    public Task<MessageParserResult<MarketActivityRecord, RequestChangeAccountingPointCharacteristicsTransaction>> ParseAsync(Stream message, CimFormat cimFormat)
    {
        var parser = _parsers.FirstOrDefault(parser => parser.HandledFormat.Equals(cimFormat));
        if (parser is null) throw new InvalidOperationException($"No message parser found for message format '{cimFormat}'");
        return parser.ParseAsync(message);
    }
}

#pragma warning restore
