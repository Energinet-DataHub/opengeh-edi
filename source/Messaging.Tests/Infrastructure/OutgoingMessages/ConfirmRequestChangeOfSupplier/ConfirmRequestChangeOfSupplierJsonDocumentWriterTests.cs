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
using Messaging.Domain.OutgoingMessages.ConfirmRequestChangeOfSupplier;
using Messaging.Infrastructure.Common;
using Messaging.Infrastructure.Configuration;
using Messaging.Infrastructure.Configuration.Serialization;
using Messaging.Infrastructure.OutgoingMessages.ConfirmRequestChangeOfSupplier;
using Messaging.Tests.Infrastructure.OutgoingMessages.Asserts;
using Xunit;

namespace Messaging.Tests.Infrastructure.OutgoingMessages.ConfirmRequestChangeOfSupplier;

public class ConfirmRequestChangeOfSupplierJsonDocumentWriterTests
{
    private const string DocumentType = "ConfirmRequestChangeOfSupplier_MarketDocument";
    private readonly ConfirmChangeOfSupplierJsonMessageWriter _messageWriter;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly IMarketActivityRecordParser _marketActivityRecordParser;
    private JsonSchemaProvider _schemaProvider;

    public ConfirmRequestChangeOfSupplierJsonDocumentWriterTests()
    {
        _systemDateTimeProvider = new SystemDateTimeProvider();
        _marketActivityRecordParser = new MarketActivityRecordParser(new Serializer());
        _messageWriter = new ConfirmChangeOfSupplierJsonMessageWriter(_marketActivityRecordParser);
        _schemaProvider = new JsonSchemaProvider(new CimJsonSchemas());
    }

    [Fact]
    public async Task Document_is_valid()
    {
        var header = new MessageHeader("E03", "SenderId", "DDZ", "ReceiverId", "DDQ", Guid.NewGuid().ToString(), _systemDateTimeProvider.Now());
        var marketActivityRecords = new List<MarketActivityRecord>()
        {
            new(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "FakeMarketEvaluationPointId"),
        };
        var message = await _messageWriter.WriteAsync(header, marketActivityRecords.Select(record => _marketActivityRecordParser.From(record)).ToList()).ConfigureAwait(false);
        await AssertMessage(message, header, marketActivityRecords).ConfigureAwait(false);
    }

    private static void AssertMarketActivityRecords(JsonDocument document, List<MarketActivityRecord> marketActivityRecords)
    {
        if (document == null) throw new ArgumentNullException(nameof(document));
        if (marketActivityRecords == null) throw new ArgumentNullException(nameof(marketActivityRecords));
        var marketActivityRecordElement = document.RootElement.GetProperty("ConfirmRequestChangeOfSupplier_MarketDocument").GetProperty("MktActivityRecord");
        foreach (var jsonElement in marketActivityRecordElement.EnumerateArray())
        {
            AssertMarketActivityRecord(jsonElement, marketActivityRecords);
        }
    }

    private static void AssertMarketActivityRecord(JsonElement jsonElement, List<MarketActivityRecord> marketActivityRecords)
    {
        var id = jsonElement.GetProperty("mRID").ToString();
        var marketActivityRecord = marketActivityRecords.FirstOrDefault(x => x.Id == id);
        Assert.NotNull(marketActivityRecord);
        Assert.Equal(marketActivityRecord!.MarketEvaluationPointId, jsonElement.GetProperty("marketEvaluationPoint.mRID").GetProperty("value").ToString());
        Assert.Equal(marketActivityRecord.OriginalTransactionId, jsonElement.GetProperty("originalTransactionIDReference_MktActivityRecord.mRID").ToString());
    }

    private async Task AssertMessage(Stream message, MessageHeader header, List<MarketActivityRecord> marketActivityRecords)
    {
        _schemaProvider = new JsonSchemaProvider(new CimJsonSchemas());
        var schema = await _schemaProvider.GetSchemaAsync<JsonSchema>("confirmrequestchangeofsupplier", "0").ConfigureAwait(false);
        if (schema == null) throw new InvalidCastException("Json schema not found for process ConfirmRequestChangeOfSupplier");
        var document = await JsonDocument.ParseAsync(message).ConfigureAwait(false);
        AssertJsonMessage.AssertConformsToSchema(document, schema, DocumentType);
        AssertJsonMessage.AssertHeader(header, document, DocumentType);
        AssertJsonMessage.AssertHasHeaderValue(document, DocumentType, "type", "414");
        AssertJsonMessage.HasReasonCode(document, DocumentType, "A01");
        AssertMarketActivityRecords(document, marketActivityRecords);
    }
}
