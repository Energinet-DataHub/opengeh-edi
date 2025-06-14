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

using System.Globalization;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.EventBuilders;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Application.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Peek;
using Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.RSM012;
using Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.RSM015;
using Energinet.DataHub.ProcessManager.Abstractions.Client;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_024.V1.Model;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_025.V1.Model;
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
        DocumentFormat.Xml,
        DocumentFormat.Ebix
    ];

    protected BundlingOptions BundlingOptions => GetService<IOptions<BundlingOptions>>().Value;

    [Theory]
    [MemberData(nameof(SupportedDocumentFormats))]
    public async Task
        AndGiven_BusinessReasonIsYearlyMetering_When_ActorPeeksMessages_Then_ReceivesOneDocumentWithCorrectContent(DocumentFormat documentFormat)
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
        var businessReason = BusinessReason.YearlyMetering;

        GivenNowIs(now);
        GivenAuthenticatedActorIs(senderActor.ActorNumber, senderActor.ActorRole);

        var transactionId = TransactionId.From("12356478912356478912356478912356478");
        var meteringPointId = MeteringPointId.From("579999993331812345");

        // Act
        await GivenRequestMeasurements(
            documentFormat: documentFormat,
            senderActor: senderActor,
            MessageId.New(),
            businessReason,
            [
                (TransactionId: transactionId,
                    PeriodStart: periodStart,
                    PeriodEnd: now,
                    MeteringPointId: meteringPointId),
            ]);

        // Assert
        var message = ThenRequestYearlyMeasurementsInputV1ServiceBusMessageIsCorrect(
            senderSpy,
            documentFormat,
            new RequestMeasurementsInputV1AssertionInput(
                RequestedForActor: senderActor,
                BusinessReason: businessReason,
                TransactionId: transactionId,
                MeteringPointId: meteringPointId,
                ReceivedAt: now));

        /*
         *  --- PART 2: Receive data from Process Manager and create RSM document ---
         */
        // Arrange
        var startDate = Instant.FromUtc(2024, 11, 28, 13, 51);
        var endDate = Instant.FromUtc(2024, 11, 29, 9, 15);

        (int Position, string QualityCode, decimal Quantity) expectedYearlyAggregatedMeasurement = (1, "A04", 1000);

        var requestYearlyMeasurementsInputV1 = message.ParseInput<RequestYearlyMeasurementsInputV1>();
        var requestMeasurementsAcceptedServiceBusMessage = RequestYearlyMeasurementsResponseBuilder
            .GenerateAcceptedFrom(
                requestYearlyMeasurementsInputV1,
                receiverActor,
                startDate,
                endDate,
                orchestrationInstanceId,
                expectedYearlyAggregatedMeasurement);

        await GivenRequestYearlyMeasurementsAcceptedIsReceived(requestMeasurementsAcceptedServiceBusMessage);

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

        await AssertMeasurementsDocumentProvider.AssertDocument(peekResult.Bundle, documentFormat)
            .MessageIdExists()
            .HasBusinessReason(businessReason.Code)
            .HasSenderId(DataHubDetails.DataHubActorNumber.Value, "A10")
            .HasSenderRole(ActorRole.MeteredDataAdministrator.Code)
            .HasReceiverId(receiverActor.ActorNumber.Value, "A10")
            .HasReceiverRole(receiverActor.ActorRole.Code)
            .HasTimestamp(InstantPattern.General.Format(whenBundleShouldBeClosed))
            .TransactionIdExists(1)
            .HasMeteringPointNumber(1, meteringPointId.Value, "A10")
            .HasMeteringPointType(1, MeteringPointType.Consumption)
            .HasOriginalTransactionIdReferenceId(1, transactionId.Value)
            .HasProduct(1, "8716867000030")
            .HasQuantityMeasureUnit(1, MeasurementUnit.KilowattHour.Code)
            .HasRegistrationDateTime(1, now.ToString())
            .HasResolution(1, Resolution.Yearly.Code)
            .HasStartedDateTime(
                1,
                startDate.ToString("yyyy-MM-dd'T'HH:mm'Z'", CultureInfo.InvariantCulture))
            .HasEndedDateTime(
                1,
                endDate.ToString("yyyy-MM-dd'T'HH:mm'Z'", CultureInfo.InvariantCulture))
            .HasPoints(
                1,
                [new AssertPointDocumentFieldsInput(
                            new RequiredPointDocumentFields(1),
                            new OptionalPointDocumentFields(
                                Quality.FromCode(expectedYearlyAggregatedMeasurement.QualityCode),
                                expectedYearlyAggregatedMeasurement.Quantity))])
            .DocumentIsValidAsync();
    }

    [Theory]
    [MemberData(nameof(SupportedDocumentFormats))]
    public async Task
        AndGiven_BusinessReasonIsPeriodicMetering_When_ActorPeeksMessages_Then_ReceivesOneDocumentWithCorrectContent(DocumentFormat documentFormat)
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
        var businessReason = BusinessReason.PeriodicMetering;

        GivenNowIs(now);
        GivenAuthenticatedActorIs(senderActor.ActorNumber, senderActor.ActorRole);

        var transactionId = TransactionId.From("12356478912356478912356478912356478");
        var meteringPointId = MeteringPointId.From("579999993331812345");

        // Act
        await GivenRequestMeasurements(
            documentFormat: documentFormat,
            senderActor: senderActor,
            MessageId.New(),
            businessReason,
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
                BusinessReason: businessReason,
                TransactionId: transactionId,
                MeteringPointId: meteringPointId,
                ReceivedAt: now));

        /*
         *  --- PART 2: Receive data from Process Manager and create RSM document ---
         */
        // Arrange
        var startDate = Instant.FromUtc(2024, 11, 28, 13, 51);
        var endDate = Instant.FromUtc(2024, 11, 29, 9, 15);

        IList<(int Position, string QualityCode, decimal Quantity)> expectedAggregatedMeasurement = new List<(int Position, string QualityCode, decimal Quantity)>()
        {
            (1, "A04", 1000),
            (2, "A04", 1100),
            (3, "A04", 1200),
            (4, "A04", 1300),
            (5, "A04", 1400),
            (6, "A04", 1500),
        };

        var requestMeasurementsInputV1 = message.ParseInput<RequestMeasurementsInputV1>();
        var requestMeasurementsAcceptedServiceBusMessage = RequestMeasurementsResponseBuilder
            .GenerateAcceptedFrom(
                requestMeasurementsInputV1,
                receiverActor,
                startDate,
                endDate,
                orchestrationInstanceId,
                expectedAggregatedMeasurement);

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

        await AssertMeasurementsDocumentProvider.AssertDocument(peekResult.Bundle, documentFormat)
            .MessageIdExists()
            .HasBusinessReason(businessReason.Code)
            .HasSenderId(DataHubDetails.DataHubActorNumber.Value, "A10")
            .HasSenderRole(ActorRole.MeteredDataAdministrator.Code)
            .HasReceiverId(receiverActor.ActorNumber.Value, "A10")
            .HasReceiverRole(receiverActor.ActorRole.Code)
            .HasTimestamp(InstantPattern.General.Format(whenBundleShouldBeClosed))
            .TransactionIdExists(1)
            .HasMeteringPointNumber(1, meteringPointId.Value, "A10")
            .HasMeteringPointType(1, MeteringPointType.Consumption)
            .HasOriginalTransactionIdReferenceId(1, transactionId.Value)
            .HasProduct(1, "8716867000030")
            .HasQuantityMeasureUnit(1, MeasurementUnit.KilowattHour.Code)
            .HasRegistrationDateTime(1, now.ToString())
            .HasResolution(1, Resolution.QuarterHourly.Code)
            .HasStartedDateTime(
                1,
                startDate.ToString("yyyy-MM-dd'T'HH:mm'Z'", CultureInfo.InvariantCulture))
            .HasEndedDateTime(
                1,
                endDate.ToString("yyyy-MM-dd'T'HH:mm'Z'", CultureInfo.InvariantCulture))
            .HasPoints(
                1,
                expectedAggregatedMeasurement.Select(
                    x => new AssertPointDocumentFieldsInput(
                        new RequiredPointDocumentFields(x.Position),
                        new OptionalPointDocumentFields(
                            Quality.FromCode(x.QualityCode),
                            x.Quantity))).ToList().AsReadOnly())
            .DocumentIsValidAsync();
    }

    [Theory]
    [MemberData(nameof(SupportedDocumentFormats))]
    public async Task
        AndGiven_BusinessReasonIsYearlyMeteringAndRequestIsRejectedByProcessManager_When_ActorPeeksMessages_Then_ReceivesOneRejectDocumentWithCorrectContent(
            DocumentFormat documentFormat)
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
        var businessReason = BusinessReason.YearlyMetering;

        GivenNowIs(now);
        GivenAuthenticatedActorIs(senderActor.ActorNumber, senderActor.ActorRole);

        var transactionId = TransactionId.From("12356478912356478912356478912356478");
        var meteringPointId = MeteringPointId.From("579999993331812345");

        // Act
        await GivenRequestMeasurements(
            documentFormat: documentFormat,
            senderActor: senderActor,
            MessageId.New(),
            businessReason,
            [
                (TransactionId: transactionId,
                    PeriodStart: periodStart,
                    PeriodEnd: now,
                    MeteringPointId: meteringPointId),
            ]);

        // Assert
        var message = ThenRequestYearlyMeasurementsInputV1ServiceBusMessageIsCorrect(
            senderSpy,
            documentFormat,
            new RequestMeasurementsInputV1AssertionInput(
                RequestedForActor: senderActor,
                BusinessReason: businessReason,
                TransactionId: transactionId,
                MeteringPointId: meteringPointId,
                ReceivedAt: now));

        /*
         *  --- PART 2: Receive data from Process Manager and create RSM document ---
         */
        // Arrange
        var expectedValidationErrorMessage = "I forbindelse med anmodning om årssum kan der kun "
                                             + "anmodes om data for forbrug og produktion/When requesting yearly "
                                             + "amount then it is only possible to request for "
                                             + "production and consumption";
        var expectedValidationErrorCode = "D18";

        var requestYearlyMeasurementsInputV1 = message.ParseInput<RequestYearlyMeasurementsInputV1>();
        var requestMeasurementsRejectedServiceBusMessage = RequestYearlyMeasurementsResponseBuilder
            .GenerateRejectedFrom(
                requestYearlyMeasurementsInputV1,
                receiverActor,
                expectedValidationErrorMessage,
                expectedValidationErrorCode,
                orchestrationInstanceId);

        await GivenRequestYearlyMeasurementsRejectedIsReceived(requestMeasurementsRejectedServiceBusMessage);

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

        await AssertRejectRequestMeasurementsDocumentProvider.AssertDocument(
            peekResult.Bundle,
            documentFormat)
            .MessageIdExists()
            .HasBusinessReason(businessReason)
            .HasSenderId(DataHubDetails.DataHubActorNumber)
            .HasSenderRole(ActorRole.MeteredDataAdministrator)
            .HasReceiverId(receiverActor.ActorNumber)
            .HasReceiverRole(receiverActor.ActorRole)
            .HasTimestamp(whenBundleShouldBeClosed)
            .HasReasonCode(ReasonCode.FullyRejected)
            .TransactionIdExists()
            .HasMeteringPointId(meteringPointId)
            .HasOriginalTransactionId(transactionId)
            .HasSerieReasonCode(expectedValidationErrorCode)
            .HasSerieReasonMessage(expectedValidationErrorMessage)
            .DocumentIsValidAsync();
    }

    [Theory]
    [MemberData(nameof(SupportedDocumentFormats))]
    public async Task
        AndGiven_BusinessReasonIsPeriodicMeteringAndRequestIsRejectedByProcessManager_When_ActorPeeksMessages_Then_ReceivesOneRejectDocumentWithCorrectContent(
            DocumentFormat documentFormat)
    {
        /*
         *  --- PART 1: Receive request and send message to Process Manager ---
         */

        // Arrange
        var senderSpy = CreateServiceBusSenderSpy(StartSenderClientNames.ProcessManagerStartSender);
        var senderSpyNotify = CreateServiceBusSenderSpy(NotifySenderClientNames.ProcessManagerNotifySender);
        const string notifyEventName = RequestMeasurementsNotifyEventV1.OrchestrationInstanceEventName;
        var senderActor = new Actor(ActorNumber.Create("5799999933318"), ActorRole.EnergySupplier);
        var receiverActor = senderActor;
        var orchestrationInstanceId = Guid.NewGuid();

        var localNow = new LocalDate(2025, 5, 23);
        var now = CreateDateInstant(localNow.Year, localNow.Month, localNow.Day);
        var oneYearAgo = localNow.Minus(Period.FromYears(1));
        var periodStart = CreateDateInstant(oneYearAgo.Year, oneYearAgo.Month, oneYearAgo.Day);
        var businessReason = BusinessReason.PeriodicMetering;

        GivenNowIs(now);
        GivenAuthenticatedActorIs(senderActor.ActorNumber, senderActor.ActorRole);

        var transactionId = TransactionId.From("12356478912356478912356478912356478");
        var meteringPointId = MeteringPointId.From("579999993331812345");

        // Act
        await GivenRequestMeasurements(
            documentFormat: documentFormat,
            senderActor: senderActor,
            MessageId.New(),
            businessReason,
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
                BusinessReason: businessReason,
                TransactionId: transactionId,
                MeteringPointId: meteringPointId,
                ReceivedAt: now));

        /*
         *  --- PART 2: Receive data from Process Manager and create RSM document ---
         */
        // Arrange
        var expectedValidationErrorMessage = "I forbindelse med anmodning om årssum kan der kun "
                                             + "anmodes om data for forbrug og produktion/When requesting yearly "
                                             + "amount then it is only possible to request for "
                                             + "production and consumption";
        var expectedValidationErrorCode = "D18";

        var requestMeasurementsInputV1 = message.ParseInput<RequestMeasurementsInputV1>();
        var requestMeasurementsRejectedServiceBusMessage = RequestMeasurementsResponseBuilder
            .GenerateRejectedFrom(
                requestMeasurementsInputV1,
                receiverActor,
                expectedValidationErrorMessage,
                expectedValidationErrorCode,
                orchestrationInstanceId);

        await GivenRequestMeasurementsRejectedIsReceived(requestMeasurementsRejectedServiceBusMessage);

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

        await AssertRejectRequestMeasurementsDocumentProvider.AssertDocument(
            peekResult.Bundle,
            documentFormat)
            .MessageIdExists()
            .HasBusinessReason(businessReason)
            .HasSenderId(DataHubDetails.DataHubActorNumber)
            .HasSenderRole(ActorRole.MeteredDataAdministrator)
            .HasReceiverId(receiverActor.ActorNumber)
            .HasReceiverRole(receiverActor.ActorRole)
            .HasTimestamp(whenBundleShouldBeClosed)
            .HasReasonCode(ReasonCode.FullyRejected)
            .TransactionIdExists()
            .HasMeteringPointId(meteringPointId)
            .HasOriginalTransactionId(transactionId)
            .HasSerieReasonCode(expectedValidationErrorCode)
            .HasSerieReasonMessage(expectedValidationErrorMessage)
            .DocumentIsValidAsync();
    }
}
