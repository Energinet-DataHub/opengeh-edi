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
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.Formats;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM012;

public class MeteredDataForMeteringPointCimJsonDocumentWriter(IMessageRecordParser parser, JavaScriptEncoder encoder)
    : IDocumentWriter
{
    private const string DocumentTypeName = "NotifyValidatedMeasureData_MarketDocument";
    private const string TypeCode = "E66";
    private readonly IMessageRecordParser _parser = parser;
    private readonly JsonWriterOptions _options = new() { Indented = true, Encoder = encoder };

    public bool HandlesFormat(DocumentFormat format)
    {
        return format == DocumentFormat.Json;
    }

    public bool HandlesType(DocumentType documentType)
    {
        return documentType == DocumentType.NotifyValidatedMeasureData;
    }

    public async Task<MarketDocumentStream> WriteAsync(
        OutgoingMessageHeader header,
        IReadOnlyCollection<string> marketActivityRecords,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var document = ParseFrom(header, marketActivityRecords);

        var stream = new MarketDocumentWriterMemoryStream();
        using var writer = new Utf8JsonWriter(stream, _options);
        JsonSerializer.Serialize(writer, document);
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);

        stream.Position = 0;
        return new MarketDocumentStream(stream);
    }

    private Document ParseFrom(OutgoingMessageHeader header, IReadOnlyCollection<string> transactions)
    {
        ArgumentNullException.ThrowIfNull(transactions);

        var meteredDataForMeasurementSeries = new Collection<MeteredDataForMeasurementSeries>();
        foreach (var activityRecord in transactions.Select(t => _parser.From<MeteredDataForMeteringPointMarketActivityRecord>(t)))
        {
            meteredDataForMeasurementSeries.Add(
                new MeteredDataForMeasurementSeries(
                    activityRecord.TransactionId.Value,
                    activityRecord.MeteringPointId,
                    activityRecord.MeteringPointType,
                    activityRecord.OriginalTransactionIdReference?.Value,
                    activityRecord.Product,
                    activityRecord.MeasurementUnit.Code,
                    activityRecord.RegistrationDateTime.ToString(),
                    new Period(
                        activityRecord.Resolution.Code,
                        new TimeInterval(
                            activityRecord.Period.Start.ToString("yyyy-MM-dd'T'HH:mm'Z'", CultureInfo.InvariantCulture),
                            activityRecord.Period.End.ToString("yyyy-MM-dd'T'HH:mm'Z'", CultureInfo.InvariantCulture)),
                        activityRecord.Measurements.Select(
                                p => new Point(
                                    p.Position,
                                    p.Quality,
                                    p.Quantity))
                            .ToList())));
        }

        return new Document(
            new MeteredDataForMeteringPoint(
                header.MessageId,
                GeneralValues.SectorTypeCode,
                header.TimeStamp.ToString(),
                BusinessReason.FromName(header.BusinessReason).Code,
                header.ReceiverId,
                header.ReceiverRole,
                header.SenderId,
                header.SenderRole,
                TypeCode,
                meteredDataForMeasurementSeries));
    }
}

internal class Document(MeteredDataForMeteringPoint meteredDataForMeteringPoint)
{
    [JsonPropertyName("NotifyValidatedMeasureData_MarketDocument")]
    public MeteredDataForMeteringPoint MeteredDataForMeteringPoint { get; init; } = meteredDataForMeteringPoint;
}

internal class MeteredDataForMeteringPoint(
    string messageId,
    string businessSectorType,
    string createdDateTime,
    string businessReasonCode,
    string receiverId,
    string receiverRole,
    string senderId,
    string senderRole,
    string typeCode,
    IReadOnlyCollection<MeteredDataForMeasurementSeries> meteredDataForMeasurementSeries)
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
    public IReadOnlyCollection<MeteredDataForMeasurementSeries> MeteredDataForMeasurementSeries { get; init; } =
        meteredDataForMeasurementSeries;
}

internal class MeteredDataForMeasurementSeries(
    string transactionId,
    string marketEvaluationPointNumber,
    MeteringPointType marketEvaluationPointType,
    string? originalTransactionIdReferenceId,
    string? product,
    string quantityMeasureUnit,
    string? registrationDateTime,
    Period period)
{
    [JsonPropertyName("mRID")]
    public string TransactionId { get; init; } = transactionId;

    [JsonPropertyName("marketEvaluationPoint.mRID")]
    public ValueObjectWithScheme MeteringPointNumber { get; init; } = ValueObjectWithScheme.Create("A10", marketEvaluationPointNumber);

    [JsonPropertyName("marketEvaluationPoint.type")]
    public ValueObject<string> MeteringPointType { get; init; } = ValueObject<string>.Create(marketEvaluationPointType.Code);

    [JsonPropertyName("originalTransactionIDReference_Series.mRID")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OriginalTransactionIdReferenceId { get; init; } = originalTransactionIdReferenceId;

    [JsonPropertyName("product")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Product { get; init; } = product;

    [JsonPropertyName("quantity_Measure_Unit.name")]
    public ValueObject<string> QuantityMeasureUnit { get; init; } = ValueObject<string>.Create(quantityMeasureUnit);

    [JsonPropertyName("registration_DateAndOrTime.dateTime")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RegistrationDateTime { get; init; } = registrationDateTime;

    [JsonPropertyName("Period")]
    public Period Period { get; init; } = period;
}

internal class Period(string resolution, TimeInterval timeInterval, IReadOnlyCollection<Point> points)
{
    [JsonPropertyName("resolution")]
    public string Resolution { get; init; } = resolution;

    [JsonPropertyName("timeInterval")]
    public TimeInterval TimeInterval { get; init; } = timeInterval;

    [JsonPropertyName("Point")]
    public IReadOnlyCollection<Point> Points { get; init; } = points;
}

internal class TimeInterval(string startedDateTime, string endedDateTime)
{
    [JsonPropertyName("start")]
    public ValueObject<string> StartedDateTime { get; init; } = ValueObject<string>.Create(startedDateTime);

    [JsonPropertyName("end")]
    public ValueObject<string> EndedDateTime { get; init; } = ValueObject<string>.Create(endedDateTime);
}

internal class Point(int position, Quality? quality, decimal? quantity)
{
    [JsonPropertyName("position")]
    public ValueObject<int> Position { get; init; } = ValueObject<int>.Create(position);

    [JsonPropertyName("quality")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ValueObject<string>? Quality { get; init; } = quality == null ? null : ValueObject<string>.Create(quality.Code);

    [JsonPropertyName("quantity")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? Quantity { get; init; } = quantity == null ? null : quantity;
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
