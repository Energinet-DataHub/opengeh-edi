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

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.CIM;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats.CIM.Json;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData.V1.Model;

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

    public bool HandlesMultipleRecords() => false;

    public async Task<MarketDocumentStream> WriteAsync(
        OutgoingMessageHeader messageHeader,
        IReadOnlyCollection<string> rawAcknowledgementV1,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var acknowledgementV1 = ParseFrom(rawAcknowledgementV1.Single());

        var stream = new MarketDocumentWriterMemoryStream();

        using var writer = new Utf8JsonWriter(stream, _options);

        writer.WriteStartObject();
        {
            writer.WritePropertyName(DocumentTypeName);
            writer.WriteStartObject();
            {
                WriteHeader(messageHeader, acknowledgementV1, writer);
                WriteReason(acknowledgementV1.Reason, writer);
                WriteInErrorPeriod(acknowledgementV1.InErrorPeriod, writer);
                WriteSeries(acknowledgementV1.Series, writer);
                WriteOriginalMktActivityRecord(acknowledgementV1.OriginalMktActivityRecord, writer);
                WriteRejectedTimeSeries(acknowledgementV1.RejectedTimeSeries, writer);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndObject();

        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        stream.Position = 0;

        return new MarketDocumentStream(stream);
    }

    private void WriteReason(IReadOnlyCollection<ReasonV1> acknowledgementRecordReasons, Utf8JsonWriter writer)
    {
        if (acknowledgementRecordReasons.Count <= 0)
        {
            return;
        }

        writer.WriteStartArray("Reason");
        foreach (var reason in acknowledgementRecordReasons)
        {
            writer.WriteStartObject();
            {
                writer.WriteObject("code", new KeyValuePair<string, string>("value", reason.Code));
                WritePropertyIfNotNull(writer, "text", reason.Text);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private void WriteInErrorPeriod(
        IReadOnlyCollection<TimePeriodV1> acknowledgementRecordTimePeriods,
        Utf8JsonWriter writer)
    {
        if (acknowledgementRecordTimePeriods.Count <= 0)
        {
            return;
        }

        writer.WriteStartArray("InError_Period");
        foreach (var timePeriod in acknowledgementRecordTimePeriods)
        {
            writer.WriteStartObject();
            {
                writer.WriteStartObject("timeInterval");
                {
                    writer.WriteObject(
                        "start",
                        new KeyValuePair<string, string>(
                            "value",
                            timePeriod.TimeInterval.Start.ToString(
                                "yyyy-MM-ddTHH:mm'Z'",
                                CultureInfo.InvariantCulture)));

                    writer.WriteObject(
                        "end",
                        new KeyValuePair<string, string>(
                            "value",
                            timePeriod.TimeInterval.End.ToString(
                                "yyyy-MM-ddTHH:mm'Z'",
                                CultureInfo.InvariantCulture)));
                }

                writer.WriteEndObject();

                WriteReason(timePeriod.Reason, writer);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private void WriteSeries(
        IReadOnlyCollection<SeriesV1> acknowledgementRecordSeries,
        Utf8JsonWriter writer)
    {
        if (acknowledgementRecordSeries.Count <= 0)
        {
            return;
        }

        writer.WriteStartArray("Series");
        foreach (var series in acknowledgementRecordSeries)
        {
            writer.WriteStartObject();
            {
                writer.WriteProperty("mRID", series.MRID);
                WriteReason(series.Reason, writer);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private void WriteOriginalMktActivityRecord(
        IReadOnlyCollection<MktActivityRecordV1> acknowledgementRecordMktActivityRecords,
        Utf8JsonWriter writer)
    {
        if (acknowledgementRecordMktActivityRecords.Count <= 0)
        {
            return;
        }

        writer.WriteStartArray("Original_MktActivityRecord");
        foreach (var mktActivityRecord in acknowledgementRecordMktActivityRecords)
        {
            writer.WriteStartObject();
            {
                writer.WriteProperty("mRID", mktActivityRecord.MRID);
                WriteReason(mktActivityRecord.Reason, writer);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private void WriteRejectedTimeSeries(
        IReadOnlyCollection<TimeSeriesV1> acknowledgementRecordTimeSeries,
        Utf8JsonWriter writer)
    {
        if (acknowledgementRecordTimeSeries.Count <= 0)
        {
            return;
        }

        writer.WriteStartArray("Rejected_TimeSeries");
        foreach (var timeSeries in acknowledgementRecordTimeSeries)
        {
            writer.WriteStartObject();
            {
                writer.WriteProperty("mRID", timeSeries.MRID);

                writer.WriteObject(
                    "version",
                    new KeyValuePair<string, string>(
                        "value",
                        timeSeries.Version));

                WriteInErrorPeriod(timeSeries.InErrorPeriod, writer);
                WriteReason(timeSeries.Reason, writer);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private void WriteHeader(
        OutgoingMessageHeader messageHeader,
        AcknowledgementV1 acknowledgementRecord,
        Utf8JsonWriter writer)
    {
        writer.WriteProperty("mRID", messageHeader.MessageId);
        writer.WriteObject(
            "businessSector.type",
            new KeyValuePair<string, string>("value", GeneralValues.SectorTypeCode));
        writer.WriteProperty("createdDateTime", messageHeader.TimeStamp.ToString());

        WritePropertyIfNotNull(
            writer,
            "received_MarketDocument.createdDateTime",
            acknowledgementRecord.ReceivedMarketDocumentCreatedDateTime?.ToString(
                "yyyy-MM-ddTHH:mm:ss'Z'",
                CultureInfo.InvariantCulture));

        WritePropertyIfNotNull(
            writer,
            "received_MarketDocument.mRID",
            acknowledgementRecord.ReceivedMarketDocumentTransactionId);

        WriteValueObjectIfNotNull(
            writer,
            "received_MarketDocument.process.processType",
            acknowledgementRecord.ReceivedMarketDocumentProcessProcessType);

        WritePropertyIfNotNull(
            writer,
            "received_MarketDocument.revisionNumber",
            acknowledgementRecord.ReceivedMarketDocumentRevisionNumber);

        WritePropertyIfNotNull(
            writer,
            "received_MarketDocument.title",
            acknowledgementRecord.ReceivedMarketDocumentTitle);

        WriteValueObjectIfNotNull(
            writer,
            "received_MarketDocument.type",
            acknowledgementRecord.ReceivedMarketDocumentType);

        writer.WriteObject(
            "receiver_MarketParticipant.mRID",
            new KeyValuePair<string, string>(
                "codingScheme",
                CimCode.CodingSchemeOf(ActorNumber.Create(messageHeader.ReceiverId))),
            new KeyValuePair<string, string>("value", messageHeader.ReceiverId));

        writer.WriteObject(
            "receiver_MarketParticipant.marketRole.type",
            new KeyValuePair<string, string>("value", messageHeader.ReceiverRole));

        writer.WriteObject(
            "sender_MarketParticipant.mRID",
            new KeyValuePair<string, string>(
                "codingScheme",
                CimCode.CodingSchemeOf(ActorNumber.Create(messageHeader.SenderId))),
            new KeyValuePair<string, string>("value", messageHeader.SenderId));

        writer.WriteObject(
            "sender_MarketParticipant.marketRole.type",
            new KeyValuePair<string, string>("value", messageHeader.SenderRole));
    }

    private void WritePropertyIfNotNull(Utf8JsonWriter writer, string property, string? value)
    {
        if (value is not null)
        {
            writer.WriteProperty(property, value);
        }
    }

    private void WriteValueObjectIfNotNull(Utf8JsonWriter writer, string @object, string? value)
    {
        if (value is not null)
        {
            writer.WriteObject(@object, new KeyValuePair<string, string>("value", value));
        }
    }

    private AcknowledgementV1 ParseFrom(string acknowledgementRecord)
    {
        ArgumentNullException.ThrowIfNull(acknowledgementRecord);

        return _parser.From<AcknowledgementV1>(acknowledgementRecord);
    }
}
