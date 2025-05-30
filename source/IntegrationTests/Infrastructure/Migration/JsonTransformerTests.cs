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

using System.Text.Json;
using AutoFixture.Xunit2;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Migration;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM012;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.RSM012;
using NodaTime.Extensions;
using NodaTime.Text;
using Xunit;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Migration;

public class JsonTransformerTests
{
    private static readonly string TestJson = """
    {
    "MeteredDataTimeSeriesDH3": {
    "Header": {
    "MessageId": "13255042",
    "DocumentType": "E66",
    "Creation": "2024-01-16T07:55:33Z",
    "EnergyBusinessProcess": "D42",
    "EnergyIndustryClassification": "23",
    "SenderIdentification": {
    "SchemeAgencyIdentifier": "9",
    "content": "5790001330552"
    },
    "RecipientIdentification": {
    "SchemeAgencyIdentifier": "9",
    "content": "5790001330595"
    },
    "EnergyBusinessProcessRole": "D3M"
    },
    "TimeSeries": [
    {
    "TimeSeriesId": "74634301_86192545",
    "OriginalMessageId": "bc8897bc5d5b4d8a9e7f72efe4b0d4c5",
    "OriginalTimeSeriesId": "e1f06dee48d842c1a48b187065e710ff",
    "EnergyTimeSeriesFunction": "9",
    "EnergyTimeSeriesProduct": "8716867000030",
    "EnergyTimeSeriesMeasureUnit": "KWH",
    "TypeOfMP": "E17",
    "SettlementMethod": "D01",
    "AggregationCriteria": {
    "MeteringPointId": "571051839308770693"
    },
    "Observation": [
    {
    "Position": 1,
    "QuantityQuality": "E01",
    "EnergyQuantity": 2.0
    },
    {
    "Position": 2,
    "QuantityQuality": "E01",
    "EnergyQuantity": 2.0
    },
    {
    "Position": 3,
    "QuantityQuality": "E01",
    "EnergyQuantity": 2.0
    },
    {
    "Position": 4,
    "QuantityQuality": "E01",
    "EnergyQuantity": 2.0
    },
    {
    "Position": 5,
    "QuantityQuality": "E01",
    "EnergyQuantity": 2.0
    },
    {
    "Position": 6,
    "QuantityQuality": "E01",
    "EnergyQuantity": 2.0
    },
    {
    "Position": 7,
    "QuantityQuality": "E01",
    "EnergyQuantity": 2.0
    },
    {
    "Position": 8,
    "QuantityQuality": "E01",
    "EnergyQuantity": 2.0
    },
    {
    "Position": 9,
    "QuantityQuality": "E01",
    "EnergyQuantity": 2.0
    },
    {
    "Position": 10,
    "QuantityQuality": "E01",
    "EnergyQuantity": 2.0
    },
    {
    "Position": 11,
    "QuantityQuality": "E01",
    "EnergyQuantity": 2.0
    },
    {
    "Position": 12,
    "QuantityQuality": "E01",
    "EnergyQuantity": 2.0
    },
    {
    "Position": 13,
    "QuantityQuality": "E01",
    "EnergyQuantity": 2.0
    },
    {
    "Position": 14,
    "QuantityQuality": "E01",
    "EnergyQuantity": 2.0
    },
    {
    "Position": 15,
    "QuantityQuality": "E01",
    "EnergyQuantity": 2.0
    },
    {
    "Position": 16,
    "QuantityQuality": "E01",
    "EnergyQuantity": 2.0
    },
    {
    "Position": 17,
    "QuantityQuality": "E01",
    "EnergyQuantity": 2.0
    },
    {
    "Position": 18,
    "QuantityQuality": "E01",
    "EnergyQuantity": 2.0
    },
    {
    "Position": 19,
    "QuantityQuality": "E01",
    "EnergyQuantity": 2.0
    },
    {
    "Position": 20,
    "QuantityQuality": "E01",
    "EnergyQuantity": 2.0
    },
    {
    "Position": 21,
    "QuantityQuality": "E01",
    "EnergyQuantity": 2.0
    },
    {
    "Position": 22,
    "QuantityQuality": "E01",
    "EnergyQuantity": 2.0
    },
    {
    "Position": 23,
    "QuantityQuality": "E01",
    "EnergyQuantity": 2.0
    },
    {
    "Position": 24,
    "QuantityQuality": "E01",
    "EnergyQuantity": 2.0
    }
    ],
    "TimeSeriesPeriod": {
    "ResolutionDuration": "PT1H",
    "Start": "2023-12-25T23:00:00Z",
    "End": "2023-12-26T23:00:00Z"
    },
    "TransactionInsertDate": "2024-01-16T08:55:14Z",
    "TimeSeriesStatus": "2"
    }
    ]
    }
    }
    """;

    [Fact]
    public async Task TransformJsonMessage_ValidJson_ReturnsMeteredDataForMeteringPointMarketActivityRecords()
    {
        // Arrange
        var transformer = new TimeSeriesJsonToEbixTransformer();
        var serializer = new Serializer();
        var writer = new MeteredDataForMeteringPointEbixDocumentWriter(new MessageRecordParser(serializer));
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Deserialize the JSON into the Root object for processing
        var root = JsonSerializer.Deserialize<Root>(TestJson, options) ?? throw new Exception("Root is null.");

        // Extract the header and time series data
        var header = root.MeteredDataTimeSeriesDH3.Header;
        var series = root.MeteredDataTimeSeriesDH3.TimeSeries;

        // Create the outgoing message header using the deserialized header data
        var outgoingMessageHeader = new OutgoingMessageHeader(
            BusinessReason.FromCode(header.EnergyBusinessProcess).Name,
            header.SenderIdentification.Content,
            ActorRole.GridAccessProvider.Code,
            header.RecipientIdentification.Content,
            ActorRole.MeteredDataResponsible.Code,
            header.MessageId,
            null,
            header.Creation.ToInstant());

        // Act
        var meteredDataForMeteringPointMarketActivityRecords = transformer.TransformJsonMessage(header.Creation.ToInstant(), series);

        // Write the document using the MeteredDataForMeteringPointMarketActivityRecords and the outgoing message header
        var stream = await writer.WriteAsync(
            outgoingMessageHeader,
            meteredDataForMeteringPointMarketActivityRecords.Select(serializer.Serialize).ToList(),
            CancellationToken.None);

        // Assert

        // Assert the correctness of the written document
        await NotifyValidatedMeasureDataDocumentAsserter.AssertCorrectDocumentAsync(DocumentFormat.Ebix, stream.Stream, new NotifyValidatedMeasureDataDocumentAssertionInput(
                    RequiredHeaderDocumentFields: new RequiredHeaderDocumentFields(
                        BusinessReasonCode: "D42",
                        ReceiverId: "5790001330595",
                        ReceiverScheme: "A10",
                        SenderId: "5790001330552",
                        SenderScheme: "A10",
                        SenderRole: "DGL",
                        ReceiverRole: "MDR",
                        Timestamp: InstantPattern.General.Format(header.Creation.ToInstant())),
                    OptionalHeaderDocumentFields: new OptionalHeaderDocumentFields(
                        BusinessSectorType: "23",
                        AssertSeriesDocumentFieldsInput: [
                            new AssertSeriesDocumentFieldsInput(
                                1,
                                RequiredSeriesFields: new RequiredSeriesFields(
                                    MeteringPointNumber: "571051839308770693",
                                    MeteringPointScheme: "A10",
                                    MeteringPointType: MeteringPointType.FromCode("E17"),
                                    QuantityMeasureUnit: "KWH",
                                    RequiredPeriodDocumentFields: new RequiredPeriodDocumentFields(
                                        Resolution: "PT1H",
                                        StartedDateTime: "2023-12-25T23:00:00Z",
                                        EndedDateTime: "2023-12-26T23:00:00Z",
                                        Points: series.First().Observation
                                            .Select(eo => new AssertPointDocumentFieldsInput(
                                                new RequiredPointDocumentFields(eo.Position),
                                                new OptionalPointDocumentFields(
                                                    Quantity: eo.EnergyQuantity,
                                                    Quality: eo.QuantityQuality != null ? Quality.Measured : null)))
                                            .ToList())),
                                OptionalSeriesFields: new OptionalSeriesFields(
                                    OriginalTransactionIdReferenceId: null,
                                    RegistrationDateTime: "2022-12-17T09:30:00Z",
                                    InDomain: null,
                                    OutDomain: null,
                                    Product: "8716867000030")),
                        ])));
    }

    [Fact]
    public async Task Transform_ValidJson_ReturnsTransformedJson()
    {
        // Arrange
        var serializer = new Serializer();
        var writer = new MeteredDataForMeteringPointEbixDocumentWriter(new MessageRecordParser(serializer));

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };

        // Deserialize the JSON into the Root object for processing
        var root = JsonSerializer.Deserialize<Root>(TestJson, options);

        if (root == null)
            throw new Exception("Root is null.");

        // Extract the header and time series data
        var header = root.MeteredDataTimeSeriesDH3.Header;
        var series = root.MeteredDataTimeSeriesDH3.TimeSeries;

        // Transform the time series data into MeteredDataForMeteringPointMarketActivityRecord objects which are used for writing the document
        var meteredDataForMeteringPointMarketActivityRecords = series.Select(timeSeries => new MeteredDataForMeteringPointMarketActivityRecord(
                TransactionId.From(timeSeries.OriginalTimeSeriesId),
                timeSeries.AggregationCriteria.MeteringPointId,
                MeteringPointType.FromCode(timeSeries.TypeOfMP),
                null,
                timeSeries.EnergyTimeSeriesProduct,
                MeasurementUnit.FromCode(timeSeries.EnergyTimeSeriesMeasureUnit),
                header.Creation.ToInstant(),
                Resolution.FromCode(timeSeries.TimeSeriesPeriod.ResolutionDuration),
                new Period(timeSeries.TimeSeriesPeriod.Start.ToInstant(), timeSeries.TimeSeriesPeriod.End.ToInstant()),
                timeSeries.Observation.Select(x =>
                {
                    var tryGetNameFromEbixCode = Quality.TryGetNameFromEbixCode(x.QuantityQuality, x.QuantityQuality);
                    return new PointActivityRecord(
                        x.Position,
                        Quality.FromName(tryGetNameFromEbixCode!),
                        x.EnergyQuantity);
                }).ToList()))
            .ToList();

        // Create the outgoing message header using the deserialized header data
        var outgoingMessageHeader = new OutgoingMessageHeader(
            BusinessReason.FromCode(header.EnergyBusinessProcess).Name,
            header.SenderIdentification.Content,
            ActorRole.GridAccessProvider.Code,
            header.RecipientIdentification.Content,
            ActorRole.MeteredDataResponsible.Code,
            header.MessageId,
            null,
            header.Creation.ToInstant());

        // Write the document using the MeteredDataForMeteringPointMarketActivityRecords and the outgoing message header
        var stream = await writer.WriteAsync(outgoingMessageHeader, meteredDataForMeteringPointMarketActivityRecords.Select(serializer.Serialize).ToList(), CancellationToken.None);

        // Assert the correctness of the written document
        await NotifyValidatedMeasureDataDocumentAsserter.AssertCorrectDocumentAsync(DocumentFormat.Ebix, stream.Stream, new NotifyValidatedMeasureDataDocumentAssertionInput(
                    RequiredHeaderDocumentFields: new RequiredHeaderDocumentFields(
                        BusinessReasonCode: "D42",
                        ReceiverId: "5790001330595",
                        ReceiverScheme: "A10",
                        SenderId: "5790001330552",
                        SenderScheme: "A10",
                        SenderRole: "DGL",
                        ReceiverRole: "MDR",
                        Timestamp: InstantPattern.General.Format(header.Creation.ToInstant())),
                    OptionalHeaderDocumentFields: new OptionalHeaderDocumentFields(
                        BusinessSectorType: "23",
                        AssertSeriesDocumentFieldsInput: [
                            new AssertSeriesDocumentFieldsInput(
                                1,
                                RequiredSeriesFields: new RequiredSeriesFields(
                                    MeteringPointNumber: "571051839308770693",
                                    MeteringPointScheme: "A10",
                                    MeteringPointType: MeteringPointType.FromCode("E17"),
                                    QuantityMeasureUnit: "KWH",
                                    RequiredPeriodDocumentFields: new RequiredPeriodDocumentFields(
                                        Resolution: "PT1H",
                                        StartedDateTime: "2023-12-25T23:00:00Z",
                                        EndedDateTime: "2023-12-26T23:00:00Z",
                                        Points: series.First().Observation
                                            .Select(eo => new AssertPointDocumentFieldsInput(
                                                new RequiredPointDocumentFields(eo.Position),
                                                new OptionalPointDocumentFields(
                                                    Quantity: eo.EnergyQuantity,
                                                    Quality: eo.QuantityQuality != null ? Quality.Measured : null)))
                                            .ToList())),
                                OptionalSeriesFields: new OptionalSeriesFields(
                                    OriginalTransactionIdReferenceId: null,
                                    RegistrationDateTime: "2022-12-17T09:30:00Z",
                                    InDomain: null,
                                    OutDomain: null,
                                    Product: "8716867000030")),
                        ])));
    }
}
