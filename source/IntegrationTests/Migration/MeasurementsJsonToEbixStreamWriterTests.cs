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
using Energinet.DataHub.EDI.B2BApi.Migration;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Serialization;
using Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.RSM012;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Migration;

public class MeasurementsJsonToEbixStreamWriterTests
{
    [Fact]
    public async Task Given_ValidJson_When_WriteStreamAsync_Then_ReturnsValidMarketDocumentStream()
    {
        // Arrange
        var timeSeriesJsonToEbixStreamWriter = new MeasurementsJsonToEbixStreamWriter(
            new Serializer(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            new MeasurementsToMarketActivityRecordTransformer());

        // Act
        var stream = await timeSeriesJsonToEbixStreamWriter.WriteStreamAsync(JsonPayloadConstants.SingleTimeSeriesWithSingleObservation);

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
                        Timestamp: "2024-01-16T07:55:33Z"),
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
                                        Points: new List<AssertPointDocumentFieldsInput>
                                        {
                                            new(
                                                new RequiredPointDocumentFields(1),
                                                new OptionalPointDocumentFields(BuildingBlocks.Domain.Models.Quality.Measured, 2.0M)),
                                        })),
                                OptionalSeriesFields: new OptionalSeriesFields(
                                    OriginalTransactionIdReferenceId: null,
                                    RegistrationDateTime: "2022-12-17T09:30:00Z",
                                    InDomain: null,
                                    OutDomain: null,
                                    Product: "8716867000030")),
                        ])));
    }
}
