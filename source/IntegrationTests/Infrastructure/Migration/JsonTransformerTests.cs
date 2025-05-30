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

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Migration;

public class JsonTransformerTests
{
    [Fact]
    public async Task TransformJsonMessage_ValidJson_ReturnsMeteredDataForMeteringPointMarketActivityRecords()
    {
        // Arrange
        var transformer = new TimeSeriesJsonToMarketActivityRecordTransformer();
        var serializer = new Serializer();
        var writer = new MeteredDataForMeteringPointEbixDocumentWriter(new MessageRecordParser(serializer));
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Deserialize the JSON into the Root object for processing
        var root = JsonSerializer.Deserialize<Root>(JsonPayloadConstants.OneTimeSeries, options) ?? throw new Exception("Root is null.");

        // Extract the header and time series data
        var series = root.MeteredDataTimeSeriesDH3.TimeSeries;
        var header = root.MeteredDataTimeSeriesDH3.Header;
        var creationTime = header.Creation.ToInstant();

        // Create the outgoing message header using the deserialized header data
        var outgoingMessageHeader = new OutgoingMessageHeader(
            BusinessReason.FromCode(header.EnergyBusinessProcess).Name,
            header.SenderIdentification.Content,
            ActorRole.GridAccessProvider.Code,
            header.RecipientIdentification.Content,
            ActorRole.MeteredDataResponsible.Code,
            header.MessageId,
            null,
            creationTime);

        // Act
        var meteredDataForMeteringPointMarketActivityRecords = transformer.TransformJsonMessage(creationTime, series);

        // Write the document using the MeteredDataForMeteringPointMarketActivityRecords and the outgoing message header
        var stream = await writer.WriteAsync(
            outgoingMessageHeader,
            meteredDataForMeteringPointMarketActivityRecords.Select(serializer.Serialize).ToList(),
            CancellationToken.None);

        // Assert

        // Assert the correctness of the written document
        await NotifyValidatedMeasureDataDocumentAsserter.AssertCorrectDocumentAsync(
            DocumentFormat.Ebix, stream.Stream, new NotifyValidatedMeasureDataDocumentAssertionInput(
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
