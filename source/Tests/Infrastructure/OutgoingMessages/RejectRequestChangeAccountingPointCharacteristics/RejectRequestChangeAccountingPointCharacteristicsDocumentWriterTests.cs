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
using Application.Configuration;
using Application.OutgoingMessages.Common;
using DocumentValidation;
using DocumentValidation.CimXml;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.RejectRequestChangeOfSupplier;
using Infrastructure.Configuration;
using Infrastructure.Configuration.Serialization;
using Infrastructure.OutgoingMessages.Common;
using Infrastructure.OutgoingMessages.RejectRequestChangeAccountingPointCharacteristics;
using Tests.Infrastructure.OutgoingMessages.Asserts;
using Xunit;
using MarketActivityRecord = Domain.OutgoingMessages.RejectRequestChangeAccountingPointCharacteristics.MarketActivityRecord;

namespace Tests.Infrastructure.OutgoingMessages.RejectRequestChangeAccountingPointCharacteristics;

public class RejectRequestChangeAccountingPointCharacteristicsDocumentWriterTests
{
    private readonly RejectRequestChangeAccountingPointCharacteristicsMessageWriter _xmlMessageWriter;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly IMessageRecordParser _messageRecordParser;
    private ISchemaProvider? _schemaProvider;

    public RejectRequestChangeAccountingPointCharacteristicsDocumentWriterTests()
    {
        _systemDateTimeProvider = new SystemDateTimeProvider();
        _messageRecordParser = new MessageRecordParser(new Serializer());
        _xmlMessageWriter = new RejectRequestChangeAccountingPointCharacteristicsMessageWriter(_messageRecordParser);
    }

    [Fact]
    public async Task Document_is_valid()
    {
        var header = new MessageHeader(ProcessType.MoveIn.Name, "SenderId", "DDZ", "ReceiverId", "DDQ", Guid.NewGuid().ToString(), _systemDateTimeProvider.Now());
        var marketActivityRecords = new List<MarketActivityRecord>()
        {
            new(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "FakeMarketEvaluationPointId", new List<Reason>()
            {
                new Reason("Reason1", "999"),
                new Reason("Reason2", "999"),
            }),
            new(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "FakeMarketEvaluationPointId",
            new List<Reason>()
            {
                new Reason("Reason3", "999"),
                new Reason("Reason4", "999"),
            }),
        };

        var message = await _xmlMessageWriter.WriteAsync(header, marketActivityRecords.Select(record => _messageRecordParser.From(record)).ToList()).ConfigureAwait(false);

        await AssertMessage(message, header, marketActivityRecords).ConfigureAwait(false);
    }

    private static void AssertMarketActivityRecords(List<MarketActivityRecord> marketActivityRecords, XDocument document)
    {
        AssertXmlMessage.AssertMarketActivityRecordCount(document, 2);
        foreach (var activityRecord in marketActivityRecords)
        {
            var marketActivityRecord = AssertXmlMessage.GetMarketActivityRecordById(document, activityRecord.Id)!;

            Assert.NotNull(marketActivityRecord);
            AssertXmlMessage.AssertMarketActivityRecordValue(marketActivityRecord, "originalTransactionIDReference_MktActivityRecord.mRID", activityRecord.OriginalTransactionId);
            AssertXmlMessage.AssertMarketActivityRecordValue(marketActivityRecord, "marketEvaluationPoint.mRID", activityRecord.MarketEvaluationPointId);
            AssertXmlMessage.AssertReasons(marketActivityRecord, activityRecord.Reasons.ToList());
        }
    }

    private async Task AssertMessage(Stream message, MessageHeader header, List<MarketActivityRecord> marketActivityRecords)
    {
        _schemaProvider = new CimXmlSchemaProvider();
        var document = XDocument.Load(message);
        AssertXmlMessage.AssertHeader(header, document);
        AssertXmlMessage.AssertHasHeaderValue(document, "type", "A80");
        AssertXmlMessage.HasReasonCode(document, "A02");

        AssertMarketActivityRecords(marketActivityRecords, document);

        var schema = await _schemaProvider.GetSchemaAsync<XmlSchema>("rejectrequestchangeaccountingpointcharacteristics", "0.1")
            .ConfigureAwait(false);
        await AssertXmlMessage.AssertConformsToSchemaAsync(message, schema!).ConfigureAwait(false);
    }
}
