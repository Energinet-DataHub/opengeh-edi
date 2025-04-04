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

using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Json;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.CIM;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.CIM.Json;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM009;

[SuppressMessage(
    "StyleCop.CSharp.ReadabilityRules",
    "SA1118:Parameter should not span multiple lines",
    Justification = "Readability")]
public sealed class AcknowledgementJsonDocumentWriter(IMessageRecordParser parser, JavaScriptEncoder encoder)
    : IDocumentWriter
{
    private const string DocumentTypeName = "Acknowledgement_MarketDocument";

    private readonly IMessageRecordParser _parser = parser;
    private readonly JsonWriterOptions _options = new() { Indented = true, Encoder = encoder };

    public bool HandlesFormat(DocumentFormat format) => format == DocumentFormat.Json;

    public bool HandlesType(DocumentType documentType) => documentType == DocumentType.Acknowledgement;

    public async Task<MarketDocumentStream> WriteAsync(
        OutgoingMessageHeader messageHeader,
        IReadOnlyCollection<string> rawAcknowledgement,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var acknowledgements = ParseFrom(rawAcknowledgement);

        var stream = new MarketDocumentWriterMemoryStream();

        using var writer = new Utf8JsonWriter(stream, _options);

        writer.WriteStartObject();
        {
            writer.WritePropertyName(DocumentTypeName);
            writer.WriteStartObject();
            {
                WriteHeader(messageHeader, writer);
                writer.WriteStartArray("Series");
                foreach (var acknowledgement in acknowledgements)
                {
                    WriteSeries(acknowledgement, writer);
                }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        writer.WriteEndObject();

        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        stream.Position = 0;

        return new MarketDocumentStream(stream);
    }

    private void WriteSeries(
        RejectedForwardMeteredDataRecord rejectedForwardMeteredDataRecord,
        Utf8JsonWriter writer)
    {
            writer.WriteStartObject();
            {
                writer.WriteProperty("mRID", rejectedForwardMeteredDataRecord.OriginalTransactionIdReference.Value);
                WriteReasons(rejectedForwardMeteredDataRecord.RejectReasons, writer);
            }

            writer.WriteEndObject();
    }

    private void WriteReasons(IReadOnlyCollection<RejectReason> rejectReasons, Utf8JsonWriter writer)
    {
        writer.WriteStartArray("Reason");
        foreach (var rejectReason in rejectReasons)
        {
            writer.WriteStartObject();
            {
                writer.WriteObject("code", new KeyValuePair<string, string>("value", rejectReason.ErrorCode));
                WritePropertyIfNotNull(writer, "text", rejectReason.ErrorMessage);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private void WriteHeader(
        OutgoingMessageHeader messageHeader,
        Utf8JsonWriter writer)
    {
        writer.WriteProperty("mRID", messageHeader.MessageId);
        writer.WriteObject(
            "businessSector.type",
            new KeyValuePair<string, string>("value", GeneralValues.SectorTypeCode));

        writer.WriteProperty("createdDateTime", messageHeader.TimeStamp.ToString());

        writer.WriteProperty("received_MarketDocument.mRID", messageHeader.RelatedToMessageId!);

        writer.WriteObject(
            "received_MarketDocument.process.processType",
            new KeyValuePair<string, string>("value", BusinessReason.FromName(messageHeader.BusinessReason).Code));

        writer.WriteObject(
            "sender_MarketParticipant.mRID",
            new KeyValuePair<string, string>(
                "codingScheme",
                CimCode.CodingSchemeOf(ActorNumber.Create(messageHeader.SenderId))),
            new KeyValuePair<string, string>("value", messageHeader.SenderId));

        writer.WriteObject(
            "sender_MarketParticipant.marketRole.type",
            new KeyValuePair<string, string>("value", messageHeader.SenderRole));

        writer.WriteObject(
            "receiver_MarketParticipant.mRID",
            new KeyValuePair<string, string>(
                "codingScheme",
                CimCode.CodingSchemeOf(ActorNumber.Create(messageHeader.ReceiverId))),
            new KeyValuePair<string, string>("value", messageHeader.ReceiverId));

        writer.WriteObject(
            "receiver_MarketParticipant.marketRole.type",
            new KeyValuePair<string, string>("value", messageHeader.ReceiverRole));
    }

    private void WritePropertyIfNotNull(Utf8JsonWriter writer, string property, string? value)
    {
        if (value is not null)
        {
            writer.WriteProperty(property, value);
        }
    }

    private List<RejectedForwardMeteredDataRecord> ParseFrom(IReadOnlyCollection<string> marketActivityPayloads)
    {
        ArgumentNullException.ThrowIfNull(marketActivityPayloads);

        var marketActivityRecords = new List<RejectedForwardMeteredDataRecord>();
        foreach (var acknowledgementRecord in marketActivityPayloads)
        {
            marketActivityRecords.Add(_parser.From<RejectedForwardMeteredDataRecord>(acknowledgementRecord));
        }

        return marketActivityRecords;
    }
}
