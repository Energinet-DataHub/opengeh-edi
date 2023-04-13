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
using System.Text.Json;
using System.Threading.Tasks;
using Application.OutgoingMessages.Common;
using Domain.Documents;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.ConfirmRequestChangeOfSupplier;
using Infrastructure.OutgoingMessages.Common.Json;

namespace Infrastructure.OutgoingMessages.ConfirmRequestChangeOfSupplier;

public class ConfirmChangeOfSupplierJsonMessageWriter : IMessageWriter
{
    private const string DocumentType = "ConfirmRequestChangeOfSupplier_MarketDocument";
    private const string TypeCode = "414";
    private readonly IMessageRecordParser _parser;

    public ConfirmChangeOfSupplierJsonMessageWriter(IMessageRecordParser parser)
    {
        _parser = parser;
    }

    public bool HandlesFormat(DocumentFormat format)
    {
        return format == DocumentFormat.Json;
    }

    public bool HandlesType(DocumentType documentType)
    {
        if (documentType == null) throw new ArgumentNullException(nameof(documentType));
        return documentType.Name.Equals(DocumentType.Split("_")[0], StringComparison.OrdinalIgnoreCase);
    }

    public async Task<Stream> WriteAsync(MessageHeader header, IReadOnlyCollection<string> marketActivityRecords)
    {
        var stream = new MemoryStream();
        var options = new JsonWriterOptions() { Indented = true };
        using var writer = new Utf8JsonWriter(stream, options);

        WriteHeader(header, writer);
        WriteMarketActivityRecords(marketActivityRecords, writer);
        WriteEnd(writer);
        await writer.FlushAsync().ConfigureAwait(false);
        stream.Position = 0;
        return stream;
    }

    private static void WriteEnd(Utf8JsonWriter writer)
    {
        writer.WriteEndObject();
    }

    private static void WriteHeader(MessageHeader header, Utf8JsonWriter writer)
    {
        JsonHeaderWriter.Write(header, DocumentType, TypeCode, "A01", writer);
    }

    private void WriteMarketActivityRecords(IReadOnlyCollection<string> marketActivityRecords, Utf8JsonWriter writer)
    {
        if (marketActivityRecords == null) throw new ArgumentNullException(nameof(marketActivityRecords));
        if (writer == null) throw new ArgumentNullException(nameof(writer));

        writer.WritePropertyName("MktActivityRecord");
        writer.WriteStartArray();

        foreach (var marketActivityRecord in ParseFrom<MarketActivityRecord>(marketActivityRecords))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("mRID");
            writer.WriteStringValue(marketActivityRecord.Id);
            writer.WritePropertyName("marketEvaluationPoint.mRID");
            writer.WriteStartObject();
            writer.WritePropertyName("codingScheme");
            writer.WriteStringValue("A10");
            writer.WritePropertyName("value");
            writer.WriteStringValue(marketActivityRecord.MarketEvaluationPointId);
            writer.WriteEndObject();
            writer.WritePropertyName("originalTransactionIDReference_MktActivityRecord.mRID");
            writer.WriteStringValue(marketActivityRecord.OriginalTransactionId);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private IReadOnlyCollection<TMarketActivityRecord> ParseFrom<TMarketActivityRecord>(IReadOnlyCollection<string> payloads)
    {
        if (payloads == null) throw new ArgumentNullException(nameof(payloads));
        var marketActivityRecords = new List<TMarketActivityRecord>();
        foreach (var payload in payloads)
        {
            marketActivityRecords.Add(_parser.From<TMarketActivityRecord>(payload));
        }

        return marketActivityRecords;
    }
}
