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
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Application.IncomingMessages.RequestAggregatedMeasureData;
using CimMessageAdapter.Messages;
using CimMessageAdapter.Messages.RequestAggregatedMeasureData;
using Domain.Documents;
using Infrastructure.IncomingMessages.RequestAggregatedMeasureData;
using Xunit;

namespace Tests.CimMessageAdapter.Messages.RequestAggregatedMeasureData;

public class MessageParserTests
{
    private static readonly string PathToMessages =
        $"cimMessageAdapter{Path.DirectorySeparatorChar}Messages{Path.DirectorySeparatorChar}";

    private readonly MessageParser _messageParser;

    public MessageParserTests()
    {
        _messageParser = new MessageParser(
            new IMessageParser<Serie, RequestAggregatedMeasureDataTransaction>[]
            {
                new XmlMessageParser(),
                // TODO: add json parser: new JsonMessageParser(),
            });
    }

    public static IEnumerable<object[]> CreateMessages()
    {
        return new List<object[]> { new object[] { DocumentFormat.Xml, CreateXmlMessage() } };
        // TODO: add json parser: new object[] { DocumentFormat.Json, CreateMessages(),
    }

    public static IEnumerable<object[]> CreateBadMessages()
    {
        return new List<object[]> { new object[] { DocumentFormat.Xml, CreateBadXmlMessage() } };
        // TODO: add json parser: new object[] { DocumentFormat.Json, CreateMessages(),
    }

    [Theory]
    [MemberData(nameof(CreateMessages))]
    public async Task Can_parse(DocumentFormat format, Stream message)
    {
        var result = await _messageParser.ParseAsync(message, format, CancellationToken.None).ConfigureAwait(false);

        Assert.True(result.Success);
    }

    [Theory]
    [MemberData(nameof(CreateBadMessages))]
    public async Task Can_not_parse(DocumentFormat format, Stream message)
    {
        var result = await _messageParser.ParseAsync(message, format, CancellationToken.None).ConfigureAwait(false);

        Assert.True(result.Errors.Count > 0);
        Assert.True(result.Success == false);
    }

    #region xml messages
    private static Stream CreateXmlMessage()
    {
        var xmlDocument = XDocument.Load(
            $"{PathToMessages}xml{Path.DirectorySeparatorChar}AggregatedMeasure{Path.DirectorySeparatorChar}RequestAggregatedMeasureData.xml");
        var stream = new MemoryStream();
        xmlDocument.Save(stream);

        return stream;
    }

    private static Stream CreateBadXmlMessage()
    {
        var xmlDocument = XDocument.Load(
            $"{PathToMessages}xml{Path.DirectorySeparatorChar}AggregatedMeasure{Path.DirectorySeparatorChar}BadRequestAggregatedMeasureData.xml");
        var stream = new MemoryStream();
        xmlDocument.Save(stream);

        return stream;
    }

    #endregion
}
