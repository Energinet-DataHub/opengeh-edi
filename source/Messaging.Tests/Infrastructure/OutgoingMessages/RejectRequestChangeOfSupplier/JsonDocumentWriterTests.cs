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
using System.Text.Json;
using System.Threading.Tasks;
using Json.Schema;
using Messaging.Application.Configuration;
using Messaging.Application.OutgoingMessages.Common;
using Messaging.Application.SchemaStore;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.RejectRequestChangeOfSupplier;
using Messaging.Infrastructure.Common;
using Messaging.Infrastructure.Configuration;
using Messaging.Infrastructure.Configuration.Serialization;
using Messaging.Infrastructure.OutgoingMessages.RejectRequestChangeOfSupplier;
using Messaging.Tests.Infrastructure.OutgoingMessages.Asserts;
using Xunit;

namespace Messaging.Tests.Infrastructure.OutgoingMessages.RejectRequestChangeOfSupplier;

public class JsonDocumentWriterTests
{
    private const string DocumentType = "RejectRequestChangeOfSupplier_MarketDocument";
    private readonly RejectRequestChangeOfSupplierJsonDocumentWriter _documentWriter;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly IMarketActivityRecordParser _marketActivityRecordParser;
    private readonly JsonSchemaProvider _schemaProvider;

    public JsonDocumentWriterTests()
    {
        _systemDateTimeProvider = new SystemDateTimeProvider();
        _marketActivityRecordParser = new MarketActivityRecordParser(new Serializer());
        _documentWriter = new RejectRequestChangeOfSupplierJsonDocumentWriter(_marketActivityRecordParser);
        _schemaProvider = new JsonSchemaProvider();
    }

    [Fact]
    public async Task Document_is_valid()
    {
        var header = new MessageHeader("E03", "SenderId", "DDZ", "ReceiverId", "DDQ", "messageID", _systemDateTimeProvider.Now());
        var marketActivityRecords = new List<MarketActivityRecord>()
        {
            new("mrid1", "OriginalTransactionId", "FakeMarketEvaluationPointId", new List<Reason>()
            {
                new Reason("Reason1", "999"),
                new Reason("Reason2", "999"),
            }),
            new("mrid2", "FakeTransactionId", "FakeMarketEvaluationPointId",
            new List<Reason>()
            {
                new Reason("Reason3", "999"),
                new Reason("Reason4", "999"),
            }),
        };

        var message = await _documentWriter.WriteAsync(
            header,
            marketActivityRecords.Select(record => _marketActivityRecordParser.From(record)).ToList()).ConfigureAwait(false);

        await AssertMessage(message, header, marketActivityRecords).ConfigureAwait(false);
    }

    private static void AssertMarketActivityRecords(JsonDocument document, List<MarketActivityRecord> marketActivityRecords)
    {
        if (document == null) throw new ArgumentNullException(nameof(document));
        if (marketActivityRecords == null) throw new ArgumentNullException(nameof(marketActivityRecords));

        var marketActivityRecordElement = document.RootElement.GetProperty(DocumentType).GetProperty("MktActivityRecord");

        Assert.Equal(2, marketActivityRecordElement.EnumerateArray().Count());

        foreach (var jsonElement in marketActivityRecordElement.EnumerateArray())
        {
            var id = jsonElement.GetProperty("mRID").ToString();
            var marketActivityRecord = marketActivityRecords.FirstOrDefault(x => x.Id == id);
            AssertMarketActivityRecord(jsonElement, marketActivityRecord!);
        }
    }

    private static void AssertMarketActivityRecord(JsonElement jsonElement, MarketActivityRecord originalMarketActivityRecord)
    {
        Assert.Equal(originalMarketActivityRecord.MarketEvaluationPointId, jsonElement.GetProperty("marketEvaluationPoint.mRID").GetProperty("value").ToString());
        Assert.Equal(originalMarketActivityRecord.OriginalTransactionId, jsonElement.GetProperty("originalTransactionIDReference_MktActivityRecord.mRID").ToString());

        var index = 0;
        foreach (var element in jsonElement.GetProperty("Reason").EnumerateArray())
        {
            var text = element.GetProperty("text");
            Assert.Equal(originalMarketActivityRecord.Reasons.ToList()[index].Text, text.ToString());
            var code = element.GetProperty("code");
            Assert.Equal(originalMarketActivityRecord.Reasons.ToList()[index].Code, code.GetProperty("value").ToString());
            index++;
        }
    }

    private async Task AssertMessage(Stream message, MessageHeader header, List<MarketActivityRecord> marketActivityRecords)
    {
        var schema = await _schemaProvider.GetSchemaAsync<JsonSchema>("rejectrequestchangeofsupplier", "0").ConfigureAwait(false);
        if (schema == null) throw new InvalidCastException("Json schema not found for process RejectRequestChangeOfSupplier");
        var document = await JsonDocument.ParseAsync(message).ConfigureAwait(false);
        AssertJsonMessage.AssertConformsToSchema(document, schema, DocumentType);
        AssertJsonMessage.AssertHeader(header, document, DocumentType);
        AssertJsonMessage.AssertHasHeaderValue(document, DocumentType, "type", "414");
        AssertMarketActivityRecords(document, marketActivityRecords);
    }
}
