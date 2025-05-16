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

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM018;

public sealed class MissingMeasurementJsonDocumentWriter(IMessageRecordParser parser, JavaScriptEncoder encoder)
    : IDocumentWriter
{
    private const string DocumentTypeName = "ReminderOfMissingMeasureData_MarketDocument";
    private const string TypeCode = "D24";

    private readonly IMessageRecordParser _parser = parser;
    private readonly JsonWriterOptions _options = new() { Indented = true, Encoder = encoder };

    public bool HandlesFormat(DocumentFormat format) => format == DocumentFormat.Json;

    public bool HandlesType(DocumentType documentType) => documentType == DocumentType.ReminderOfMissingMeasureData;

    public async Task<MarketDocumentStream> WriteAsync(
        OutgoingMessageHeader header,
        IReadOnlyCollection<string> marketActivityRecords,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var missingMeasurements = ParseFrom(marketActivityRecords);

        var document = new Document(
            new MissingMeasurements(
                header.MessageId,
                GeneralValues.SectorTypeCode,
                header.TimeStamp.ToString(),
                BusinessReason.FromName(header.BusinessReason).Code,
                header.ReceiverId,
                header.ReceiverRole,
                header.SenderId,
                header.SenderRole,
                TypeCode,
                [
                    .. missingMeasurements.Select(mm =>
                        new IndividualMissingMeasurements(
                            mm.TransactionId.Value,
                            mm.Date.ToString(),
                            mm.MeteringPointId.Value)),
                ]));

        var stream = new MarketDocumentWriterMemoryStream();
        using var writer = new Utf8JsonWriter(stream, _options);

        JsonSerializer.Serialize(writer, document);

        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        stream.Position = 0;

        return new MarketDocumentStream(stream);
    }

    private List<MissingMeasurementMarketActivityRecord> ParseFrom(IReadOnlyCollection<string> marketActivityPayloads)
    {
        ArgumentNullException.ThrowIfNull(marketActivityPayloads);

        var marketActivityRecords = new List<MissingMeasurementMarketActivityRecord>();
        foreach (var acknowledgementRecord in marketActivityPayloads)
        {
            marketActivityRecords.Add(_parser.From<MissingMeasurementMarketActivityRecord>(acknowledgementRecord));
        }

        return marketActivityRecords;
    }
}

internal class Document(MissingMeasurements missingMeasurements)
{
    [JsonPropertyName("ReminderOfMissingMeasureData_MarketDocument")]
    public MissingMeasurements MissingMeasurement { get; init; } = missingMeasurements;
}

internal class MissingMeasurements(
    string messageId,
    string businessSectorType,
    string createdDateTime,
    string businessReasonCode,
    string receiverId,
    string receiverRole,
    string senderId,
    string senderRole,
    string typeCode,
    IReadOnlyCollection<IndividualMissingMeasurements> individualMissingMeasurements)
{
    [JsonPropertyName("mRID")]
    public string MessageId { get; init; } = messageId;

    [JsonPropertyName("businessSector.type")]
    public ValueObject<string> BusinessSectorType { get; init; } = ValueObject<string>.Create(businessSectorType);

    [JsonPropertyName("createdDateTime")]
    public string CreatedDateTime { get; init; } = createdDateTime;

    [JsonPropertyName("process.processType")]
    public ValueObject<string> BusinessReasonCode { get; init; } = ValueObject<string>.Create(businessReasonCode);

    [JsonPropertyName("receiver_MarketParticipant.mRID")]
    public ValueObjectWithScheme ReceiverNumber { get; init; } = ValueObjectWithScheme.Create("A10", receiverId);

    [JsonPropertyName("receiver_MarketParticipant.marketRole.type")]
    public ValueObject<string> ReceiverRole { get; init; } = ValueObject<string>.Create(receiverRole);

    [JsonPropertyName("sender_MarketParticipant.mRID")]
    public ValueObjectWithScheme SenderNumber { get; init; } = ValueObjectWithScheme.Create("A10", senderId);

    [JsonPropertyName("sender_MarketParticipant.marketRole.type")]
    public ValueObject<string> SenderRole { get; init; } = ValueObject<string>.Create(senderRole);

    [JsonPropertyName("type")]
    public ValueObject<string> Type { get; init; } = ValueObject<string>.Create(typeCode);

    [JsonPropertyName("Series")]
    public IReadOnlyCollection<IndividualMissingMeasurements> IndividualMissingMeasurements { get; init; } =
        individualMissingMeasurements;
}

internal class IndividualMissingMeasurements(
    string transactionId,
    string? requestDateAndOrTime,
    string? marketEvaluationPoint)
{
    [JsonPropertyName("mRID")]
    public string TransactionId { get; init; } = transactionId;

    [JsonPropertyName("request_DateAndOrTime.dateTime")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RequestDateAndOrTime { get; init; } = requestDateAndOrTime;

    [JsonPropertyName("MarketEvaluationPoint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyCollection<MeasurementPointReference>? MarketEvaluationPoint { get; init; } =
        marketEvaluationPoint is null
            ? null
            : [new MeasurementPointReference(marketEvaluationPoint)];
}

internal class MeasurementPointReference(string measurementPointId)
{
    [JsonPropertyName("mRID")]
    public ValueObjectWithScheme MeasurementPointId { get; init; } =
        ValueObjectWithScheme.Create("A10", measurementPointId);
}

internal class ValueObject<T>(T value)
{
    [JsonPropertyName("value")]
    public T Value { get; init; } = value;

    internal static ValueObject<T> Create(T value)
    {
        return new ValueObject<T>(value);
    }
}

internal class ValueObjectWithScheme(string codingScheme, string value)
{
    [JsonPropertyName("codingScheme")]
    public string CodingScheme { get; init; } = codingScheme;

    [JsonPropertyName("value")]
    public string Value { get; init; } = value;

    internal static ValueObjectWithScheme Create(string codingScheme, string value)
    {
        return new ValueObjectWithScheme(codingScheme, value);
    }
}
