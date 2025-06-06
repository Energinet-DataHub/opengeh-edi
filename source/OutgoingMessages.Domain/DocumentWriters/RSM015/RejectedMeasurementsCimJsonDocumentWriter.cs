﻿// Copyright 2020 Energinet DataHub A/S
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

using System.Collections.ObjectModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.CIM.Json;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM015;

public sealed class RejectedMeasurementsCimJsonDocumentWriter(
    IMessageRecordParser parser,
    JavaScriptEncoder encoder) : IDocumentWriter
{
    private const string DocumentTypeName = "RejectRequestValidatedMeasureData_MarketDocument";
    private const string TypeCode = "ERR";
    private readonly IMessageRecordParser _parser = parser;
    private readonly JsonWriterOptions _options = new() { Indented = true, Encoder = encoder };

    public bool HandlesFormat(DocumentFormat format)
    {
        return format == DocumentFormat.Json;
    }

    public bool HandlesType(DocumentType documentType)
    {
        ArgumentNullException.ThrowIfNull(documentType);
        return documentType == DocumentType.RejectRequestMeasurements;
    }

    public async Task<MarketDocumentStream> WriteAsync(
        OutgoingMessageHeader header,
        IReadOnlyCollection<string> marketActivityRecords,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var stream = new MarketDocumentWriterMemoryStream();

        using var writer = new Utf8JsonWriter(stream, _options);

        CimJsonHeaderWriter.Write(header, DocumentTypeName, TypeCode, ReasonCode.FullyRejected.Code, writer);
        WriteSeries(marketActivityRecords, writer);
        writer.WriteEndObject();
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        stream.Position = 0;
        return new MarketDocumentStream(stream);
    }

    private void WriteSeries(IReadOnlyCollection<string> marketActivityRecords, Utf8JsonWriter writer)
    {
        ArgumentNullException.ThrowIfNull(marketActivityRecords);
        ArgumentNullException.ThrowIfNull(writer);

        writer.WritePropertyName("Series");
        writer.WriteStartArray();

        foreach (var series in ParseFrom(marketActivityRecords))
        {
            writer.WriteStartObject();

            writer.WriteProperty("mRID", series.TransactionId.Value);

            writer.WritePropertyName("marketEvaluationPoint.mRID");
            writer.WriteStartObject();
            writer.WriteProperty("codingScheme", "A10");
            writer.WriteProperty("value", series.MeteringPointId.Value);
            writer.WriteEndObject();

            writer.WriteProperty(
                "originalTransactionIDReference_Series.mRID",
                series.OriginalTransactionIdReference.Value);

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

    private ReadOnlyCollection<RejectedMeasurementsRecord> ParseFrom(IReadOnlyCollection<string> payloads)
    {
        ArgumentNullException.ThrowIfNull(payloads);

        return payloads
            .Select(payload => _parser.From<RejectedMeasurementsRecord>(payload))
            .ToList()
            .AsReadOnly();
    }
}
