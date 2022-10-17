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
using System.Threading.Tasks;
using Messaging.Application.OutgoingMessages.Common;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.RejectRequestChangeOfSupplier;
using Messaging.Infrastructure.OutgoingMessages.Common.Json;
using Newtonsoft.Json;

namespace Messaging.Infrastructure.OutgoingMessages.RejectRequestChangeOfSupplier;

public class RejectRequestChangeOfSupplierJsonDocumentWriter : IDocumentWriter
{
    private const string DocumentType = "RejectRequestChangeOfSupplier_MarketDocument";
    private const string TypeCode = "414";
    private readonly IMarketActivityRecordParser _parser;

    public RejectRequestChangeOfSupplierJsonDocumentWriter(IMarketActivityRecordParser parser)
    {
        _parser = parser;
    }

    public bool HandlesDocumentFormat(CimFormat format)
    {
        return format == CimFormat.Json;
    }

    public bool HandlesDocumentType(DocumentType documentType)
    {
        if (documentType == null) throw new ArgumentNullException(nameof(documentType));
        return documentType.Name.Equals(DocumentType.Split("_")[0], StringComparison.OrdinalIgnoreCase);
    }

    public async Task<Stream> WriteAsync(MessageHeader header, IReadOnlyCollection<string> marketActivityRecords)
    {
        var stream = new MemoryStream();
        var streamWriter = new StreamWriter(stream, leaveOpen: true);
        using var writer = new JsonTextWriter(streamWriter);

        WriteHeader(header, writer);
        WriteMarketActitivyRecords(marketActivityRecords, writer);
        WriteEnd(writer);
        await streamWriter.FlushAsync().ConfigureAwait(false);
        stream.Position = 0;
        return stream;
    }

    private static void WriteHeader(MessageHeader header, JsonTextWriter writer)
    {
        JsonHeaderWriter.Write(header, DocumentType, TypeCode, "A02", writer);
    }

    private static void WriteEnd(JsonTextWriter writer)
    {
        writer.WriteEndObject();
    }

    private void WriteMarketActitivyRecords(IReadOnlyCollection<string> marketActivityRecords, JsonTextWriter writer)
    {
        if (marketActivityRecords == null) throw new ArgumentNullException(nameof(marketActivityRecords));
        if (writer == null) throw new ArgumentNullException(nameof(writer));

        writer.WritePropertyName("MktActivityRecord");
        writer.WriteStartArray();

        foreach (var marketActivityRecord in ParseFrom<MarketActivityRecord>(marketActivityRecords))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("mRID");
            writer.WriteValue(marketActivityRecord.Id);

            writer.WritePropertyName("marketEvaluationPoint.mRID");
            writer.WriteStartObject();
            writer.WritePropertyName("codingScheme");
            writer.WriteValue("A10");
            writer.WritePropertyName("value");
            writer.WriteValue(marketActivityRecord.MarketEvaluationPointId);
            writer.WriteEndObject();

            writer.WritePropertyName("originalTransactionIDReference_MktActivityRecord.mRID");
            writer.WriteValue(marketActivityRecord.OriginalTransactionId);

            writer.WritePropertyName("Reason");
            writer.WriteStartArray();

            foreach (var reason in marketActivityRecord.Reasons)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("code");
                writer.WriteStartObject();
                writer.WritePropertyName("value");
                writer.WriteValue(reason.Code);
                writer.WriteEndObject();
                writer.WritePropertyName("text");
                writer.WriteValue(reason.Text);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
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
