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
using Energinet.DataHub.EDI.Application.OutgoingMessages.Common;
using Energinet.DataHub.EDI.Domain.Documents;
using Energinet.DataHub.EDI.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.Domain.OutgoingMessages.RejectedRequestAggregatedMeasureData;
using Energinet.DataHub.EDI.Domain.Transactions.Aggregations;
using Energinet.DataHub.EDI.Infrastructure.OutgoingMessages.Common;
using Energinet.DataHub.EDI.Infrastructure.OutgoingMessages.Common.Json;
using DocumentFormat = Energinet.DataHub.EDI.Domain.Documents.DocumentFormat;

namespace Energinet.DataHub.EDI.Infrastructure.OutgoingMessages.RejectRequestAggregatedMeasureData;

public class RejectRequestAggregatedMeasureDataJsonDocumentWriter : IDocumentWriter
{
    private const string DocumentType = "RejectRequestAggregatedMeasureData_MarketDocument";
    private const string TypeCode = "ERR";
    private readonly IMessageRecordParser _parser;

    public RejectRequestAggregatedMeasureDataJsonDocumentWriter(IMessageRecordParser parser)
    {
        _parser = parser;
    }

    public bool HandlesFormat(DocumentFormat format)
    {
        return format == DocumentFormat.Json;
    }

    public bool HandlesType(DocumentType documentType)
    {
        return documentType == Domain.Documents.DocumentType.RejectRequestAggregatedMeasureData;
    }

    public async Task<Stream> WriteAsync(MessageHeader header, IReadOnlyCollection<string> marketActivityRecords)
    {
        var stream = new MemoryStream();
        var options = new JsonWriterOptions() { Indented = true };
        using var writer = new Utf8JsonWriter(stream, options);

        JsonHeaderWriter.Write(header, DocumentType, TypeCode, CimCode.Of(ReasonCode.FullyRejected), writer);
        WriteSeries(marketActivityRecords, writer);
        writer.WriteEndObject();
        await writer.FlushAsync().ConfigureAwait(false);
        stream.Position = 0;
        return stream;
    }

    private void WriteSeries(IReadOnlyCollection<string> marketActivityRecords, Utf8JsonWriter writer)
    {
        if (marketActivityRecords == null) throw new ArgumentNullException(nameof(marketActivityRecords));
        if (writer == null) throw new ArgumentNullException(nameof(writer));

        writer.WritePropertyName("Series");
        writer.WriteStartArray();

        foreach (var series in ParseFrom(marketActivityRecords))
        {
            writer.WriteStartObject();

            writer.WriteProperty("mRID", series.TransactionId.ToString());
            writer.WriteProperty("originalTransactionIDReference_Series.mRID", series.OriginalTransactionIdReference);

            writer.WritePropertyName("Reason");
            writer.WriteStartArray();
            foreach (var rejectReason in series.RejectReasons)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("code");
                writer.WriteStartObject();
                writer.WriteProperty("value", rejectReason.ErrorCode);
                writer.WriteEndObject();

                writer.WriteProperty("text", rejectReason.ErrorMessage);

                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private IReadOnlyCollection<RejectedTimeSerie> ParseFrom(IReadOnlyCollection<string> payloads)
    {
        if (payloads == null) throw new ArgumentNullException(nameof(payloads));
        var timeSeries = new List<RejectedTimeSerie>();
        foreach (var payload in payloads)
        {
            timeSeries.Add(_parser.From<RejectedTimeSerie>(payload));
        }

        return timeSeries;
    }
}
