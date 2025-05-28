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
using Energinet.DataHub.EDI.OutgoingMessages.Application.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.IntegrationTests.DocumentAsserters;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Peek;
using Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.RSM012;
using Energinet.DataHub.ProcessManager.Abstractions.Client;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_024.V1.Model;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Options;
using NodaTime;
using NodaTime.Text;
using Xunit;
using Xunit.Abstractions;
using Period = NodaTime.Period;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IncomingRequests;

public class GivenRequestMeasurementsTests(
    IntegrationTestFixture integrationTestFixture,
    ITestOutputHelper testOutputHelper)
    : RequestMeasurementsBehaviourTestBase(integrationTestFixture, testOutputHelper)
{
    public static TheoryData<DocumentFormat> SupportedDocumentFormats =>
    [
        DocumentFormat.Json,
        // DocumentFormat.Xml,
        // DocumentFormat.Ebix
    ];

    protected BundlingOptions BundlingOptions => GetService<IOptions<BundlingOptions>>().Value;

    [Theory]
    [MemberData(nameof(SupportedDocumentFormats))]
    public async Task
        When_ActorPeeksMessages_Then_ReceivesOneDocumentWithCorrectContent(DocumentFormat documentFormat)
    {
        /*
         *  --- PART 1: Receive request and send message to Process Manager ---
         */

        // Arrange
        var senderSpy = CreateServiceBusSenderSpy(StartSenderClientNames.ProcessManagerStartSender);
        var senderSpyNotify = CreateServiceBusSenderSpy(NotifySenderClientNames.ProcessManagerNotifySender);
        const string notifyEventName = RequestYearlyMeasurementsNotifyEventV1.OrchestrationInstanceEventName;
        var senderActor = new Actor(ActorNumber.Create("5799999933318"), ActorRole.EnergySupplier);
        var receiverActor = senderActor;
        var orchestrationInstanceId = Guid.NewGuid();

        var localNow = new LocalDate(2025, 5, 23);
        var now = CreateDateInstant(localNow.Year, localNow.Month, localNow.Day);
        var oneYearAgo = localNow.Minus(Period.FromYears(1));
        var periodStart = CreateDateInstant(oneYearAgo.Year, oneYearAgo.Month, oneYearAgo.Day);

        GivenNowIs(now);
        GivenAuthenticatedActorIs(senderActor.ActorNumber, senderActor.ActorRole);

        var transactionId = TransactionId.From("12356478912356478912356478912356478");
        var meteringPointId = MeteringPointId.From("579999993331812345");

        // Act
        await GivenRequestMeasurements(
            documentFormat: documentFormat,
            senderActor: senderActor,
            MessageId.New(),
            [
                (TransactionId: transactionId,
                    PeriodStart: periodStart,
                    PeriodEnd: now,
                    MeteringPointId: meteringPointId),
            ]);

        // Assert
        var message = ThenRequestMeasurementsInputV1ServiceBusMessageIsCorrect(
            senderSpy,
            documentFormat,
            new RequestMeasurementsInputV1AssertionInput(
                RequestedForActor: senderActor,
                BusinessReason: BusinessReason.PeriodicMetering,
                TransactionId: transactionId,
                MeteringPointId: meteringPointId,
                ReceivedAt: now));

        /*
         *  --- PART 2: Receive data from Process Manager and create RSM document ---
         */
        // Arrange
        var startDate = Instant.FromUtc(2024, 11, 28, 13, 51);
        var endDate = Instant.FromUtc(2024, 11, 29, 9, 15);

        var expectedEnergyObservations = new List<(int Position, string QualityName, decimal Quantity)>
        {
            (1, "A04", 1), (2, "A04", 2), (3, "A04", 3), (4, "A04", 4), (5, "A04", 5), (6, "A04", 6), (7, "A04", 7),
            (8, "A04", 8), (9, "A04", 9), (10, "A04", 10), (11, "A04", 11), (12, "A04", 12), (13, "A04", 13),
            (14, "A04", 14), (15, "A04", 15), (16, "A04", 16), (17, "A04", 17), (18, "A04", 18), (19, "A04", 19),
            (20, "A04", 20),
        };

        var requestYearlyMeasurementsInputV1 = message.ParseInput<RequestYearlyMeasurementsInputV1>();
        var requestMeasurementsAcceptedServiceBusMessage = RequestMeasurementsResponseBuilder
            .GenerateAcceptedFrom(
                requestYearlyMeasurementsInputV1,
                receiverActor,
                startDate,
                endDate,
                orchestrationInstanceId,
                expectedEnergyObservations);

        await GivenRequestMeasurementsAcceptedIsReceived(requestMeasurementsAcceptedServiceBusMessage);

        AssertCorrectProcessManagerNotification(
            senderSpyNotify.LatestMessage!,
            new NotifyOrchestrationInstanceEventV1AssertionInput(
                orchestrationInstanceId,
                notifyEventName));

        // Act
        var whenBundleShouldBeClosed = now.Plus(Duration.FromSeconds(BundlingOptions.BundleMessagesOlderThanSeconds));
        GivenNowIs(whenBundleShouldBeClosed);
        await GivenBundleMessagesHasBeenTriggered();

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
                        ReceiverId: receiverActor.ActorNumber.Value,
                        ReceiverScheme: "A10",
                        SenderId: "5790001330552",
                        SenderScheme: "A10",
                        SenderRole: "DGL",
                        ReceiverRole: "DDQ",
                        Timestamp: InstantPattern.General.Format(whenBundleShouldBeClosed)),
                    OptionalHeaderDocumentFields: new OptionalHeaderDocumentFields(
                        BusinessSectorType: "23",
                        AssertSeriesDocumentFieldsInput: [
                            new AssertSeriesDocumentFieldsInput(
                                1,
                                RequiredSeriesFields: new RequiredSeriesFields(
                                    MeteringPointNumber: meteringPointId.Value,
                                    MeteringPointScheme: "A10",
                                    MeteringPointType: MeteringPointType.FromCode("E17"),
                                    QuantityMeasureUnit: "KWH",
                                    RequiredPeriodDocumentFields: new RequiredPeriodDocumentFields(
                                        Resolution: Resolution.QuarterHourly.Code,
                                        StartedDateTime: startDate.ToString("yyyy-MM-dd'T'HH:mm'Z'", CultureInfo.InvariantCulture),
                                        EndedDateTime: endDate.ToString("yyyy-MM-dd'T'HH:mm'Z'", CultureInfo.InvariantCulture),
                                        Points: expectedEnergyObservations
                                            .Select(eo => new AssertPointDocumentFieldsInput(
                                                new RequiredPointDocumentFields(eo.Position),
                                                new OptionalPointDocumentFields(
                                                    Quantity: eo.Quantity,
                                                    Quality: Quality.FromCode(eo.QualityName!))))
                                            .ToList())),
                                OptionalSeriesFields: new OptionalSeriesFields(
                                    OriginalTransactionIdReferenceId: transactionId.Value,
                                    RegistrationDateTime: endDate.ToString(),
                                    InDomain: null,
                                    OutDomain: null,
                                    Product: "8716867000030")),
                        ])));
    }
}
