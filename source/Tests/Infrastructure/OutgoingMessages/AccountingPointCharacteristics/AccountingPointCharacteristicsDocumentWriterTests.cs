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
using Domain.Actors;
using Domain.OutgoingMessages;
using Infrastructure.Configuration;
using Infrastructure.Configuration.Serialization;
using Infrastructure.OutgoingMessages.AccountingPointCharacteristics;
using Infrastructure.OutgoingMessages.Common;
using Tests.Infrastructure.OutgoingMessages.Asserts;
using Xunit;
using MarketActivityRecord = Domain.OutgoingMessages.AccountingPointCharacteristics.MarketActivityRecord;

namespace Tests.Infrastructure.OutgoingMessages.AccountingPointCharacteristics;

public class AccountingPointCharacteristicsDocumentWriterTests
{
    private readonly AccountingPointCharacteristicsMessageWriter _messageWriter;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly IMessageRecordParser _messageRecordParser;
    private readonly SampleData _sampleData;
    private ISchemaProvider? _schemaProvider;

    public AccountingPointCharacteristicsDocumentWriterTests()
    {
        _systemDateTimeProvider = new SystemDateTimeProvider();
        _messageRecordParser = new MessageRecordParser(new Serializer());
        _messageWriter = new AccountingPointCharacteristicsMessageWriter(_messageRecordParser);
        _sampleData = new SampleData();
    }

    [Fact]
    public async Task Document_is_valid()
    {
        var header = new MessageHeader(ProcessType.MoveIn.Name, "SenderId", "DDZ", "ReceiverId", MarketRole.EnergySupplier.Name, Guid.NewGuid().ToString(), _systemDateTimeProvider.Now());
        var marketActivityRecord = _sampleData.CreateMarketActivityRecord();
        var marketActivityRecords = new List<MarketActivityRecord>()
        {
            marketActivityRecord,
        };
        var message = await _messageWriter.WriteAsync(header, marketActivityRecords.Select(record => _messageRecordParser.From(record)).ToList()).ConfigureAwait(false);
        await AssertMessage(message, header, marketActivityRecords).ConfigureAwait(false);
    }

    private static void AssertMarketActivityRecords(List<MarketActivityRecord> marketActivityRecords, XDocument document)
    {
        AssertXmlMessage.AssertMarketActivityRecordCount(document, 1);
        foreach (var activityRecord in marketActivityRecords)
        {
            var marketActivityRecord = AssertXmlMessage.GetMarketActivityRecordById(document, activityRecord.Id)!;

            Assert.NotNull(marketActivityRecord);
            AssertXmlMessage.AssertMarketActivityRecordValue(marketActivityRecord, "mRID", activityRecord.Id);
            AssertXmlMessage.AssertMarketActivityRecordValue(marketActivityRecord, "validityStart_DateAndOrTime.dateTime", activityRecord.ValidityStartDate.ToString());
        }
    }

    private async Task AssertMessage(Stream message, MessageHeader header, List<MarketActivityRecord> marketActivityRecords)
    {
        var document = XDocument.Load(message);
        AssertXmlMessage.AssertHeader(header, document);
        AssertXmlMessage.AssertHasHeaderValue(document, "type", "E07");

        AssertMarketActivityRecords(marketActivityRecords, document);

        await AssertConformsToSchema(message).ConfigureAwait(false);
    }

    private async Task AssertConformsToSchema(Stream message)
    {
        _schemaProvider = new CimXmlSchemaProvider();
        var schema = await _schemaProvider.GetSchemaAsync<XmlSchema>("accountingpointcharacteristics", "0.1")
            .ConfigureAwait(false);
        await AssertXmlMessage.AssertConformsToSchemaAsync(message, schema!).ConfigureAwait(false);
    }
}
