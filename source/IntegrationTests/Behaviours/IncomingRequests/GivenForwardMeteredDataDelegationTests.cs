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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.EventBuilders;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MeteredDataForMeteringPoint;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Peek;
using Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.RSM009;
using Energinet.DataHub.ProcessManager.Abstractions.Client;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_021.ForwardMeteredData.V1.Model;
using FluentAssertions;
using FluentAssertions.Execution;
using NodaTime;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Behaviours.IncomingRequests;

public class GivenForwardMeteredDataDelegationTests(
    IntegrationTestFixture integrationTestFixture,
    ITestOutputHelper testOutputHelper)
    : MeteredDataForMeteringPointBehaviourTestBase(integrationTestFixture, testOutputHelper)
{
    public static TheoryData<DocumentFormat> SupportedRejectDocumentFormats =>
    [
        DocumentFormat.Json,
        DocumentFormat.Xml,
        DocumentFormat.Ebix,
    ];

    [Theory]
    [MemberData(nameof(SupportedRejectDocumentFormats))]
    public async Task AndGiven_InvalidPeriod_When_ActorPeeksMessages_Then_ReceivesOneRejectDocumentWithCorrectContent(DocumentFormat documentFormat)
    {
        /*
         *  --- PART 1: Receive request and send message to Process Manager ---
         */

        // Arrange
        var senderSpyStart = CreateServiceBusSenderSpy(StartSenderClientNames.Brs021ForwardMeteredDataStartSender);
        var senderSpyNotify = CreateServiceBusSenderSpy(NotifySenderClientNames.Brs021ForwardMeteredDataNotifySender);
        var senderActor = (ActorNumber: ActorNumber.Create("1111111111111"), ActorRole: ActorRole.Delegated);
        var orchestrationInstanceId = Guid.NewGuid();
        var resolution = Resolution.Hourly;
        var notifyEventName = ForwardMeteredDataNotifyEventV1.OrchestrationInstanceEventName;
        var messageId = MessageId.New();
        var transactionId = TransactionId.New();

        var registeredAt = Instant.FromUtc(2022, 12, 17, 9, 30, 00);
        var startDate = Instant.FromUtc(2024, 11, 28, 13, 15);
        var endDate = Instant.FromUtc(2024, 11, 29, 9, 15);

        var expectedEnergyObservations = new List<(int Position, string? QualityName, decimal? Quantity)>
        {
            (1, "A04", 1), (2, "A04", 2), (3, "A04", 3), (4, "A04", 4), (5, "A04", 5), (6, "A04", 6), (7, "A04", 7),
            (8, "A04", 8), (9, "A04", 9), (10, "A04", 10), (11, "A04", 11), (12, "A04", 12), (13, "A04", 13),
            (14, "A04", 14), (15, "A04", 15), (16, "A04", 16), (17, "A04", 17), (18, "A04", 18), (19, "A04", 19),
            (20, "A04", 20), (21, "A04", 21),
        };

        var whenMessagesAreEnqueued = Instant.FromUtc(2024, 7, 1, 14, 57, 09);
        GivenNowIs(whenMessagesAreEnqueued);
        GivenAuthenticatedActorIs(senderActor.ActorNumber, senderActor.ActorRole);

        // Act
        await GivenReceivedMeteredDataForMeteringPoint(
            documentFormat: documentFormat,
            senderActorNumber: senderActor.ActorNumber,
            messageId,
            [
                (TransactionId: transactionId.Value,
                    // Invalid Period
                    PeriodStart: endDate,
                    PeriodEnd: startDate,
                    Resolution: resolution),
            ]);

        // Assert
        var message = ThenRequestStartForwardMeteredDataCommandV1ServiceBusMessageIsCorrect(
            senderSpyStart,
            documentFormat,
            new ForwardMeteredDataInputV1AssertionInput(
                ActorNumber: senderActor.ActorNumber.Value,
                ActorRole: ActorRole.MeteredDataResponsible.Name,
                TransactionId: transactionId,
                MeteringPointId: "579999993331812345",
                MeteringPointType: MeteringPointType.Consumption.Name,
                ProductNumber: "8716867000030",
                MeasureUnit: MeasurementUnit.KilowattHour.Name,
                RegistrationDateTime: registeredAt,
                Resolution: resolution,
                StartDateTime: endDate,
                EndDateTime: startDate,
                GridAccessProviderNumber: senderActor.ActorNumber.Value,
                DelegatedGridAreas: null,
                EnergyObservations: expectedEnergyObservations
                    .Select(eo => new ForwardMeteredDataInputV1.MeteredData(
                        eo.Position.ToString(),
                        eo.Quantity.HasValue ? eo.Quantity.Value.ToString(CultureInfo.InvariantCulture) : null,
                        eo.QualityName != null ? Quality.TryGetNameFromCode(eo.QualityName!, fallbackValue: eo.QualityName) : null))
                    .ToList()));

        /*
         *  --- PART 2: Receive data from Process Manager ---
         */

        // Arrange
        var forwardMeteredDataInputV1 = message.ParseInput<ForwardMeteredDataInputV1>();
        var forwardMeteredDataRejectedServiceBusMessage = ForwardMeteredDataResponseBuilder
            .GenerateRejectedFrom(
                forwardMeteredDataInputV1,
                orchestrationInstanceId,
                senderActor);

        await GivenForwardMeteredDataRequestRejectedIsReceived(forwardMeteredDataRejectedServiceBusMessage);

        AssertCorrectProcessManagerNotification(
            senderSpyNotify.LatestMessage!,
            new NotifyOrchestrationInstanceEventV1AssertionInput(
                orchestrationInstanceId,
                notifyEventName));

        var whenBundleShouldBeClosed = whenMessagesAreEnqueued.Plus(Duration.FromSeconds(BundlingOptions.BundleMessagesOlderThanSeconds));
        GivenNowIs(whenBundleShouldBeClosed);
        await GivenBundleMessagesHasBeenTriggered();

        // Act
        var peekResults = await WhenActorPeeksAllMessages(
            senderActor.ActorNumber,
            senderActor.ActorRole,
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

        await AssertAcknowledgementDocumentProvider.AssertDocument(peekResult.Bundle, documentFormat)
            .HasSenderId(DataHubDetails.DataHubActorNumber)
            .HasSenderRole(ActorRole.MeteredDataAdministrator)
            .HasReceiverId(senderActor.ActorNumber)
            .HasReceiverRole(ActorRole.MeteredDataResponsible)
            .HasCreationDate(whenBundleShouldBeClosed)
            .HasRelatedToMessageId(messageId)
            .HasReceivedBusinessReasonCode(BusinessReason.PeriodicMetering)
            .HasOriginalTransactionId(transactionId)
            .SeriesHasReasons(new List<RejectReason>()
            {
                new("E17", "Invalid Period"),
            }.ToArray())
            .DocumentIsValidAsync();
    }
}
