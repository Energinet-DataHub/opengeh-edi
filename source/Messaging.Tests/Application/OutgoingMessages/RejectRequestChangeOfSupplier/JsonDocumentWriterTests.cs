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
using Messaging.Application.Common;
using Messaging.Application.Configuration;
using Messaging.Application.OutgoingMessages.RejectRequestChangeOfSupplier;
using Messaging.Domain.OutgoingMessages;
using Messaging.Infrastructure.Common;
using Messaging.Infrastructure.Configuration;
using Messaging.Infrastructure.Configuration.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Messaging.Tests.Application.OutgoingMessages.RejectRequestChangeOfSupplier;

public class JsonDocumentWriterTests
{
    private const string TypeCode = "E44";
    private const string DocumentType = "RejectRequestChangeOfSupplier_MarketDocument";
    private readonly RejectRequestChangeOfSupplierJsonDocumentWriter _documentWriter;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly IMarketActivityRecordParser _marketActivityRecordParser;

    public JsonDocumentWriterTests()
    {
        _systemDateTimeProvider = new SystemDateTimeProvider();
        _marketActivityRecordParser = new MarketActivityRecordParser(new Serializer());
        _documentWriter = new RejectRequestChangeOfSupplierJsonDocumentWriter(_marketActivityRecordParser);
    }

    [Fact]
    public async Task Document_is_valid()
    {
        var header = new MessageHeader("E03", "SenderId", "DDZ", "ReceiverId", "DDQ", "messageID", _systemDateTimeProvider.Now(), "A01");
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

        AssertMessage(message, header, marketActivityRecords);
    }

    private static JObject StreamToJson(Stream stream)
    {
        stream.Position = 0;
        var serializer = new JsonSerializer();
        var sr = new StreamReader(stream);
        using var jtr = new JsonTextReader(sr);
        var json = serializer.Deserialize<JObject>(jtr)!;

        return json;
    }

    private static void AssertHeader(MessageHeader header, JObject json)
    {
        var document = json.GetValue(
            DocumentType,
            StringComparison.OrdinalIgnoreCase)!;
        Assert.Equal("messageID", document.Value<string>("mRID"));
        Assert.Equal("23", GetPropertyValue(document, "businessSector.type"));
        var headerDateTime = TruncateMilliseconds(header.TimeStamp.ToDateTimeUtc());
        var documentDateTime = TruncateMilliseconds(document.Value<DateTime>("createdDateTime"));
        Assert.Equal(headerDateTime, documentDateTime);
        Assert.Equal(header.ProcessType, GetPropertyValue(document, "process.processType"));
        Assert.Equal(header.ReasonCode, GetPropertyValue(document, "reason.code"));
        Assert.Equal(header.ReceiverId, GetPropertyValue(document, "receiver_MarketParticipant.mRID"));
        Assert.Equal(header.ReceiverRole, GetPropertyValue(document, "receiver_MarketParticipant.marketRole.type"));
        Assert.Equal(header.SenderId, GetPropertyValue(document, "sender_MarketParticipant.mRID"));
        Assert.Equal(header.SenderRole, GetPropertyValue(document, "sender_MarketParticipant.marketRole.type"));
        Assert.Equal(TypeCode, GetPropertyValue(document, "type"));
    }

    private static JToken GetPropertyValue(JToken document, string propertyName)
    {
        return document.Value<JToken>(propertyName)!.Value<string>("value");
    }

    private static void AssertMarketActivityRecords(JObject json, List<MarketActivityRecord> marketActivityRecords)
    {
        if (json == null) throw new ArgumentNullException(nameof(json));
        var marketActivityRecordsJson =
            json.GetValue(
                    DocumentType,
                    StringComparison.OrdinalIgnoreCase)
                ?.Value<JArray>("MktActivityRecord")?.ToList();

        Assert.Equal(2, marketActivityRecordsJson!.Count);
        AssertMarketActivityRecord(marketActivityRecordsJson![0], marketActivityRecords[0]);
        AssertMarketActivityRecord(marketActivityRecordsJson![1], marketActivityRecords[1]);
    }

    private static void AssertMarketActivityRecord(JToken record, MarketActivityRecord originalMarketActivityRecord)
    {
        Assert.Equal(originalMarketActivityRecord.Id, record.Value<string>("mRID"));
        Assert.Equal(
            originalMarketActivityRecord.OriginalTransactionId,
            record.Value<string>("originalTransactionIDReference_MktActivityRecord.mRID"));
        Assert.Equal("FakeMarketEvaluationPointId", GetPropertyValue(record, "marketEvaluationPoint.mRID"));

        Assert.Equal(originalMarketActivityRecord.Reasons.ToList()[0].Text, GetReasonTextFromJsonMarketActivityRecord(record, 0));
        Assert.Equal(originalMarketActivityRecord.Reasons.ToList()[1].Text, GetReasonTextFromJsonMarketActivityRecord(record, 1));
    }

    private static JToken GetReasonTextFromJsonMarketActivityRecord(JToken record, int index)
    {
        return record.Children().ElementAt(3).ElementAt(0).ElementAt(index).Value<string>("text");
    }

    private static DateTime TruncateMilliseconds(DateTime time)
    {
        return new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second);
    }

    private static void AssertMessage(Stream message, MessageHeader header, List<MarketActivityRecord> marketActivityRecords)
    {
        var json = StreamToJson(message);
        AssertHeader(header, json);
        AssertMarketActivityRecords(json, marketActivityRecords);
    }
}
