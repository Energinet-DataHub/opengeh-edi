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
using System.Xml.Schema;
using Messaging.Application.Configuration;
using Messaging.Application.OutgoingMessages.Common;
using Messaging.Application.SchemaStore;
using Messaging.Domain.OutgoingMessages;
using Messaging.Infrastructure.Common;
using Messaging.Infrastructure.Configuration;
using Messaging.Infrastructure.Configuration.Serialization;
using Messaging.Infrastructure.OutgoingMessages.ConfirmRequestChangeAccountingPointCharacteristics;
using Messaging.Tests.Infrastructure.OutgoingMessages.Asserts;
using Xunit;
using MarketActivityRecord = Messaging.Domain.OutgoingMessages.ConfirmRequestChangeAccountingPointCharacteristics.MarketActivityRecord;

namespace Messaging.Tests.Infrastructure.OutgoingMessages.ConfirmRequestChangeAccountingPointCharacteristics;

public class ConfirmRequestChangeAccountingPointCharacteristicsDocumentWriterTests
{
    private readonly ConfirmRequestChangeAccountingPointCharacteristicsMessageWriter _messageWriter;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly IMessageRecordParser _messageRecordParser;
    private ISchemaProvider? _schemaProvider;

    public ConfirmRequestChangeAccountingPointCharacteristicsDocumentWriterTests()
    {
        _systemDateTimeProvider = new SystemDateTimeProvider();
        _messageRecordParser = new MessageRecordParser(new Serializer());
        _messageWriter = new ConfirmRequestChangeAccountingPointCharacteristicsMessageWriter(_messageRecordParser);
    }

    [Fact]
    public async Task Document_is_valid()
    {
        var header = new MessageHeader("D14", "SenderId", "DDZ", "ReceiverId", "DDQ", Guid.NewGuid().ToString(), _systemDateTimeProvider.Now());
        var marketActivityRecords = new List<MarketActivityRecord>()
        {
            new(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "FakeMarketEvaluationPointId"),
            new(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "FakeMarketEvaluationPointId"),
        };
        var message = await _messageWriter.WriteAsync(header, marketActivityRecords.Select(record => _messageRecordParser.From(record)).ToList()).ConfigureAwait(false);
        await AssertMessage(message, header, marketActivityRecords).ConfigureAwait(false);
    }

    private static void AssertMarketActivityRecords(List<MarketActivityRecord> marketActivityRecords, XDocument document)
    {
        AssertXmlMessage.AssertMarketActivityRecordCount(document, 2);
        foreach (var activityRecord in marketActivityRecords)
        {
            var marketActivityRecord = AssertXmlMessage.GetMarketActivityRecordById(document, activityRecord.Id)!;

            Assert.NotNull(marketActivityRecord);
            AssertXmlMessage.AssertMarketActivityRecordValue(marketActivityRecord, "mRID", activityRecord.Id);
            AssertXmlMessage.AssertMarketActivityRecordValue(marketActivityRecord, "originalTransactionIDReference_MktActivityRecord.mRID", activityRecord.OriginalTransactionId);
            AssertXmlMessage.AssertMarketActivityRecordValue(marketActivityRecord, "marketEvaluationPoint.mRID", activityRecord.MarketEvaluationPointId);
        }
    }

    private async Task AssertMessage(Stream message, MessageHeader header, List<MarketActivityRecord> marketActivityRecords)
    {
        var document = XDocument.Load(message);
        AssertXmlMessage.AssertHeader(header, document);
        AssertXmlMessage.AssertHasHeaderValue(document, "type", "E59");

        AssertMarketActivityRecords(marketActivityRecords, document);

        await AssertConformsToSchema(message).ConfigureAwait(false);
    }

    private async Task AssertConformsToSchema(Stream message)
    {
        _schemaProvider = new XmlSchemaProvider();
        var schema = await _schemaProvider.GetSchemaAsync<XmlSchema>("confirmrequestchangeaccountingpointcharacteristics", "0.1")
            .ConfigureAwait(false);
        await AssertXmlMessage.AssertConformsToSchemaAsync(message, schema!).ConfigureAwait(false);
    }
}
