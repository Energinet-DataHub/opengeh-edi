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

using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures.Extensions;
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_024;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.FeatureManagement;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.DataAccess;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_024;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_024.V1.Model;
using Energinet.DataHub.ProcessManager.Shared.Extensions;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Xunit;
using Xunit.Abstractions;
using PMValueTypes = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects;

namespace Energinet.DataHub.EDI.B2BApi.AppTests.Functions.EnqueueMessages.BRS_024;

[Collection(nameof(B2BApiAppCollectionFixture))]
public class EnqueueBrs024MessagesTests : EnqueueMessagesTestBase
{
    private readonly B2BApiAppFixture _fixture;

    public EnqueueBrs024MessagesTests(
        B2BApiAppFixture fixture,
        ITestOutputHelper testOutputHelper)
        : base(fixture, testOutputHelper)
    {
        _fixture = fixture;
        _fixture.SetTestOutputHelper(testOutputHelper);
    }

    public override async Task InitializeAsync()
    {
        _fixture.AppHostManager.ClearHostLog();
        _fixture.ServiceBusListenerMock.ResetMessageHandlersAndReceivedMessages();
        await Task.CompletedTask;
    }

    public override async Task DisposeAsync()
    {
        _fixture.ServiceBusListenerMock.ResetMessageHandlersAndReceivedMessages();
        _fixture.SetTestOutputHelper(null!);
        _fixture.DatabaseManager.CleanupDatabase();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Given_EnqueueAcceptedBrs024Message_When_MessageIsReceived_Then_AcceptedMessageIsEnqueued()
    {
        _fixture.EnsureAppHostUsesFeatureFlagValue(
        [
            new(FeatureFlagNames.PeekMeasurementMessages, true),
            new(FeatureFlagNames.UsePM28Enqueue, true)
        ]);

        // Arrange
        // => Given enqueue BRS-024 service bus message
        var senderActorNumber = DataHubDetails.DataHubActorNumber;
        var senderActorRole = ActorRole.MeteredDataAdministrator;

        const string receiverEnergySupplier = "1111111111111";
        var receiverEnergySupplierRole = ActorRole.EnergySupplier;

        var startDateTime = Instant.FromUtc(2025, 01, 31, 23, 00, 00);
        var endDateTime = startDateTime.Plus(Duration.FromDays(1));

        var eventId = EventId.From(Guid.NewGuid());

        var resolution = PMValueTypes.Resolution.QuarterHourly;
        var enqueueMessagesData = new RequestYearlyMeasurementsAcceptedV1(
            OriginalActorMessageId: Guid.NewGuid().ToString(),
            OriginalTransactionId: Guid.NewGuid().ToString(),
            MeteringPointId: "1234567890123",
            MeteringPointType: PMValueTypes.MeteringPointType.Consumption,
            ProductNumber: "test-product-number",
            AggregatedMeasurements: new List<AggregatedMeasurement>
            {
               new(
                    StartDateTime: startDateTime.ToDateTimeOffset(),
                    EndDateTime: endDateTime.ToDateTimeOffset(),
                    Resolution: resolution,
                    EnergyQuantity: 1023.45m,
                    QuantityQuality: PMValueTypes.Quality.AsProvided),
            },
            ActorNumber: ActorNumber.Create(receiverEnergySupplier).ToProcessManagerActorNumber(),
            ActorRole: receiverEnergySupplierRole.ToProcessManagerActorRole(),
            MeasureUnit: PMValueTypes.MeasurementUnit.KilowattHour,
            GridAreaCode: "804");

        var orchestrationInstanceId = Guid.NewGuid();
        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = Brs_024.Name,
            OrchestrationVersion = 1,
            OrchestrationStartedByActor = new EnqueueActorMessagesActorV1
            {
                ActorNumber = senderActorNumber.Value,
                ActorRole = senderActorRole.ToProcessManagerActorRole().ToActorRoleV1(),
            },
            OrchestrationInstanceId = orchestrationInstanceId.ToString(),
        };
        enqueueActorMessages.SetData(enqueueMessagesData);

        // Act
        await GivenEnqueueAcceptedBrs024Message(enqueueActorMessages, eventId);

        // => Then accepted message is enqueued
        // Verify the function was executed
        var functionResult = await _fixture.AppHostManager.WaitForFunctionToCompleteWithSucceededAsync(
            functionName: nameof(EnqueueTrigger_Brs_024));

        functionResult.Succeeded.Should().BeTrue("because the function should have been completed with success. Host log:\n{0}", functionResult.HostLog);

        // Verify that outgoing messages were enqueued
        await using var dbContext = _fixture.DatabaseManager.CreateDbContext<ActorMessageQueueContext>();
        var enqueuedOutgoingMessages = await dbContext.OutgoingMessages
            .Where(om => om.EventId == eventId)
            .ToListAsync();

        using var assertionScope = new AssertionScope();
        enqueuedOutgoingMessages.Should()
            .HaveCount(1)
            .And.AllSatisfy(
                (om) =>
                {
                    om.DocumentType.Should().Be(DocumentType.NotifyValidatedMeasureData);
                    om.BusinessReason.Should().Be(BusinessReason.YearlyMetering.Name);
                    om.RelatedToMessageId.Should().NotBeNull();
                    om.RelatedToMessageId!.Value.Value.Should().Be(enqueueMessagesData.OriginalActorMessageId);
                    om.Receiver.Number.Value.Should().Be(enqueueMessagesData.ActorNumber.Value);
                    om.Receiver.ActorRole.Name.Should().Be(enqueueMessagesData.ActorRole.Name);
                });

        var notifyMessageSent = await ThenNotifyOrchestrationInstanceWasSentOnServiceBusAsync(
            orchestrationInstanceId,
            RequestYearlyMeasurementsNotifyEventV1.OrchestrationInstanceEventName);
        notifyMessageSent.Should().BeTrue("Notify EnqueueActorMessagesCompleted service bus message should be sent");
    }

    [Fact]
    public async Task Given_EnqueueRejectedBrs024Message_When_MessageIsReceived_Then_RejectedMessageIsEnqueued()
    {
        _fixture.EnsureAppHostUsesFeatureFlagValue(
        [
            new(FeatureFlagNames.PeekMeasurementMessages, true),
            new(FeatureFlagNames.UsePM28Enqueue, true)
        ]);

        // Arrange
        // => Given enqueue BRS-024 rejected service bus message
        const string receiverEnergySupplier = "1111111111111";
        var receiverEnergySupplierRole = ActorRole.EnergySupplier;

        var eventId = EventId.From(Guid.NewGuid());

        var rejectedMessage = new RequestYearlyMeasurementsRejectV1(
            OriginalActorMessageId: Guid.NewGuid().ToString(),
            OriginalTransactionId: Guid.NewGuid().ToString(),
            ActorNumber: ActorNumber.Create(receiverEnergySupplier).ToProcessManagerActorNumber(),
            ActorRole: receiverEnergySupplierRole.ToProcessManagerActorRole(),
            BusinessReason: PMValueTypes.BusinessReason.PeriodicMetering,
            MeteringPointId: "1234567890123",
            ValidationErrors:
            [
                new(
                    Message:
                    "I forbindelse med anmodning om årssum kan der kun anmodes om data for forbrug og produktion/When"
                    + " requesting yearly amount then it is only possible to request for production and consumption",
                    ErrorCode: "D18"),

            ]);

        var orchestrationInstanceId = Guid.NewGuid();
        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = Brs_024.Name,
            OrchestrationVersion = 1,
            OrchestrationStartedByActor = new EnqueueActorMessagesActorV1
            {
                ActorNumber = receiverEnergySupplier,
                ActorRole = receiverEnergySupplierRole.ToProcessManagerActorRole().ToActorRoleV1(),
            },
            OrchestrationInstanceId = orchestrationInstanceId.ToString(),
        };
        enqueueActorMessages.SetData(rejectedMessage);

        // Act
        await GivenEnqueueRejectedBrs024Message(enqueueActorMessages, eventId);

        // => Then reject message is enqueued
        // Verify the function was executed
        var functionResult = await _fixture.AppHostManager.WaitForFunctionToCompleteWithSucceededAsync(
            functionName: nameof(EnqueueTrigger_Brs_024));

        functionResult.Succeeded.Should().BeTrue("because the function should have been completed with success. Host log:\n{0}", functionResult.HostLog);

        // Verify that outgoing messages were enqueued
        await using var dbContext = _fixture.DatabaseManager.CreateDbContext<ActorMessageQueueContext>();
        var enqueuedOutgoingMessages = await dbContext.OutgoingMessages
            .Where(om => om.EventId == eventId)
            .ToListAsync();

        using var assertionScope = new AssertionScope();
        enqueuedOutgoingMessages.Should()
            .HaveCount(1)
            .And.AllSatisfy(
                (om) =>
                {
                    om.DocumentType.Should().Be(DocumentType.RejectRequestMeasurements);
                    om.BusinessReason.Should().Be(BusinessReason.PeriodicMetering.Name);
                    om.RelatedToMessageId!.Value.Value.Should().Be(rejectedMessage.OriginalActorMessageId);
                    om.Receiver.Number.Value.Should().Be(rejectedMessage.ActorNumber.Value);
                    om.Receiver.ActorRole.Name.Should().Be(rejectedMessage.ActorRole.Name);
                });

        var notifyMessageSent = await ThenNotifyOrchestrationInstanceWasSentOnServiceBusAsync(
            orchestrationInstanceId,
            RequestYearlyMeasurementsNotifyEventV1.OrchestrationInstanceEventName);
        notifyMessageSent.Should().BeTrue("Notify EnqueueActorMessagesCompleted service bus message should be sent");
    }

    private async Task GivenEnqueueAcceptedBrs024Message(EnqueueActorMessagesV1 enqueueActorMessages, EventId eventId)
    {
        var serviceBusMessage = enqueueActorMessages.ToServiceBusMessage(
            subject: EnqueueActorMessagesV1.BuildServiceBusMessageSubject(enqueueActorMessages.OrchestrationName),
            idempotencyKey: eventId.Value);

        await _fixture.EdiTopicResource.SenderClient.SendMessageAsync(serviceBusMessage);
    }

    private async Task GivenEnqueueRejectedBrs024Message(EnqueueActorMessagesV1 enqueueActorMessages, EventId eventId)
    {
        var serviceBusMessage = enqueueActorMessages.ToServiceBusMessage(
            subject: EnqueueActorMessagesV1.BuildServiceBusMessageSubject(enqueueActorMessages.OrchestrationName),
            idempotencyKey: eventId.Value);

        await _fixture.EdiTopicResource.SenderClient.SendMessageAsync(serviceBusMessage);
    }
}
