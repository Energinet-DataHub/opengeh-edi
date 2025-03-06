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

using System.Globalization;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.EventBuilders;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Peek;
using Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.RSM012;
using Energinet.DataHub.ProcessManager.Client.Extensions.DependencyInjection;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData.V1.Model;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IncomingRequests;

public sealed class GivenMeteredDataForMeteringPointV2Tests(
    IntegrationTestFixture integrationTestFixture,
    ITestOutputHelper testOutputHelper)
    : MeteredDataForMeteringPointBehaviourTestBase(integrationTestFixture, testOutputHelper)
{
    public static TheoryData<DocumentFormat> SupportedDocumentFormats =>
    [
        DocumentFormat.Json,
        DocumentFormat.Xml,
    ];

    [Theory]
    [MemberData(nameof(SupportedDocumentFormats))]
    public async Task When_ActorPeeksMessage_Then_ReceivesOneDocumentWithCorrectContent(DocumentFormat documentFormat)
    {
        /*
         *  --- PART 1: Receive request and send message to Process Manager ---
         */

        // Arrange
        var senderSpy = CreateServiceBusSenderSpy(ServiceBusSenderNames.ProcessManagerTopic);
        var senderActor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: ActorRole.GridAccessProvider);
        var receiverActor = (ActorNumber: ActorNumber.Create("8100000000115"), ActorRole: ActorRole.EnergySupplier);
        var resolution = Resolution.Hourly;

        var registeredAt = Instant.FromUtc(2022, 12, 17, 9, 30, 00);
        var startDate = Instant.FromUtc(2024, 11, 28, 13, 51);
        var endDate = Instant.FromUtc(2024, 11, 29, 9, 15);

        var expectedEnergyObservations = new List<(int Position, string? QualityName, decimal? Quantity)>
        {
            (1, null, null),
            (2, "A03", null),
            (3, null, 123.456m),
            (4, "A03", 654.321m),
        };

        GivenNowIs(Instant.FromUtc(2024, 7, 1, 14, 57, 09));
        GivenAuthenticatedActorIs(senderActor.ActorNumber, senderActor.ActorRole);

        var transactionId = Guid.NewGuid().ToString("N");

        // Act
        await GivenReceivedMeteredDataForMeteringPoint(
            documentFormat: documentFormat,
            senderActorNumber: senderActor.ActorNumber,
            [
                (transactionId,
                    startDate,
                    endDate,
                    resolution),
            ]);

        // Assert
        var message = ThenRequestStartForwardMeteredDataCommandV1ServiceBusMessageIsCorrect(
            senderSpy,
            new RequestMeteredDataForMeteringPointMessageInputV1AssertionInput(
                ActorNumber: senderActor.ActorNumber.Value,
                ActorRole: senderActor.ActorRole.Name,
                TransactionId: TransactionId.From(transactionId),
                MeteringPointId: "579999993331812345",
                MeteringPointType: MeteringPointType.Consumption.Name,
                ProductNumber: "8716867000030",
                MeasureUnit: MeasurementUnit.KilowattHour.Name,
                RegistrationDateTime: registeredAt,
                Resolution: resolution,
                StartDateTime: startDate,
                EndDateTime: endDate,
                GridAccessProviderNumber: senderActor.ActorNumber.Value,
                DelegatedGridAreas: null,
                EnergyObservations: expectedEnergyObservations
                    .Select(eo => new EnergyObservation(
                        eo.Position.ToString(),
                        eo.Quantity.HasValue ? eo.Quantity.Value.ToString(CultureInfo.InvariantCulture) : null,
                        eo.QualityName))
                    .ToList()));

        /*
         *  --- PART 2: Receive data from Process Manager ---
         */

        // Arrange
        var requestMeteredDataForMeteringPointInputV1 = message.ParseInput<MeteredDataForMeteringPointMessageInputV1>();
        var requestMeteredDataForMeteringPointAcceptedServiceBusMessage = MeteredDataForMeteringPointEventBuilder
            .GenerateAcceptedFrom(requestMeteredDataForMeteringPointInputV1, receiverActor);

        await GivenForwardMeteredDataRequestAcceptedIsReceived(requestMeteredDataForMeteringPointAcceptedServiceBusMessage);

        // Act
        var peekResults = await WhenActorPeeksAllMessages(
            receiverActor.ActorNumber,
            receiverActor.ActorRole,
            documentFormat);

        // Assert
        PeekResultDto peekResult;
        using (new AssertionScope())
        {
            peekResult = peekResults
                .Should()
                .ContainSingle()
                .Subject;
        }

        await ThenNotifyValidatedMeasureDataDocumentIsCorrect(
                peekResultDocumentStream: peekResult.Bundle,
                documentFormat: documentFormat,
                assertionInput: new NotifyValidatedMeasureDataDocumentAssertionInput(
                    RequiredHeaderDocumentFields: new RequiredHeaderDocumentFields(
                        BusinessReasonCode: "E23",
                        ReceiverId: "8100000000115",
                        ReceiverScheme: "A10",
                        SenderId: "5790001330552",
                        SenderScheme: "A10",
                        SenderRole: "DGL",
                        ReceiverRole: "DDQ",
                        Timestamp: "2024-07-01T14:57:09Z"),
                    OptionalHeaderDocumentFields: new OptionalHeaderDocumentFields(
                        BusinessSectorType: "23",
                        AssertSeriesDocumentFieldsInput: [
                            new AssertSeriesDocumentFieldsInput(
                                1,
                                RequiredSeriesFields: new RequiredSeriesFields(
                                    TransactionId: TransactionId.From(
                                        string.Join(
                                            string.Empty,
                                            transactionId.Reverse())),
                                    MeteringPointNumber: "579999993331812345",
                                    MeteringPointScheme: "A10",
                                    MeteringPointType: MeteringPointType.FromCode("E17"),
                                    QuantityMeasureUnit: "KWH",
                                    RequiredPeriodDocumentFields: new RequiredPeriodDocumentFields(
                                        Resolution: "PT1H",
                                        StartedDateTime: "2024-11-28T13:51Z",
                                        EndedDateTime: "2024-11-29T09:15Z",
                                        Points: [
                                            new AssertPointDocumentFieldsInput(
                                                new RequiredPointDocumentFields(1),
                                                new OptionalPointDocumentFields(null, null)),
                                            new AssertPointDocumentFieldsInput(
                                                new RequiredPointDocumentFields(2),
                                                new OptionalPointDocumentFields("A03", null)),
                                            new AssertPointDocumentFieldsInput(
                                                new RequiredPointDocumentFields(3),
                                                new OptionalPointDocumentFields(null, 123.456M)),
                                            new AssertPointDocumentFieldsInput(
                                                new RequiredPointDocumentFields(4),
                                                new OptionalPointDocumentFields("A03", 654.321M)),
                                        ])),
                                OptionalSeriesFields: new OptionalSeriesFields(
                                    OriginalTransactionIdReferenceId: transactionId,
                                    RegistrationDateTime: "2022-12-17T09:30:00Z",
                                    InDomain: null,
                                    OutDomain: null,
                                    Product: "8716867000030")),
                        ])));
    }
}
