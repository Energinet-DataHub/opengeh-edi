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
using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.BRS_025;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.FeatureManagement;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.DataAccess;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_025;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_025.V1.Model;
using Energinet.DataHub.ProcessManager.Shared.Extensions;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Xunit;
using Xunit.Abstractions;
using PMValueTypes = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects;

namespace Energinet.DataHub.EDI.B2BApi.AppTests.Functions.EnqueueMessages.BRS_025;

[Collection(nameof(B2BApiAppCollectionFixture))]
public class EnqueueBrs025MessagesTests : EnqueueMessagesTestBase
{
    private readonly B2BApiAppFixture _fixture;

    public EnqueueBrs025MessagesTests(
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
    public async Task Given_EnqueueAcceptedBrs025Message_When_MessageIsReceived_Then_AcceptedMessageIsEnqueued()
    {
        // Arrange
        _fixture.EnsureAppHostUsesFeatureFlagValue(
        [
            new(FeatureFlagNames.PeekMeasurementMessages, true),
            new(FeatureFlagNames.UsePM28Enqueue, true)
        ]);

        const string receiverEnergySupplier = "1111111111111";
        var receiverEnergySupplierRole = ActorRole.EnergySupplier;

        var startDateTime = Instant.FromUtc(2025, 01, 31, 23, 00, 00);
        var endDateTime = startDateTime.Plus(Duration.FromDays(1));

        var eventId = EventId.From(Guid.NewGuid());
        var orchestrationInstanceId = Guid.NewGuid();

        var resolution = PMValueTypes.Resolution.QuarterHourly;
        var requestMeasurementsAcceptedV1Event = CreateRequestMeasurementsAcceptedV1(startDateTime, endDateTime, resolution, receiverEnergySupplier, receiverEnergySupplierRole);

        // Act
        await GivenEnqueueAcceptedBrs025Message(requestMeasurementsAcceptedV1Event, eventId, orchestrationInstanceId);

        // Assert
        using var assertionScope = new AssertionScope();
        await EnsureFunctionIsExecuted(nameof(EnqueueTrigger_Brs_025));
        var enqueuedOutgoingMessages = await EnsureOutgoingMessagesIsEnqueued(eventId);

        enqueuedOutgoingMessages.Should()
            .HaveCount(1)
            .And.AllSatisfy(
                (om) =>
                {
                    om.DocumentType.Should().Be(DocumentType.NotifyValidatedMeasureData);
                    om.BusinessReason.Should().Be(BusinessReason.PeriodicMetering.Name);
                    om.RelatedToMessageId.Should().NotBeNull();
                    om.RelatedToMessageId!.Value.Value.Should().Be(requestMeasurementsAcceptedV1Event.OriginalActorMessageId);
                    om.Receiver.Number.Value.Should().Be(requestMeasurementsAcceptedV1Event.ActorNumber.Value);
                    om.Receiver.ActorRole.Name.Should().Be(requestMeasurementsAcceptedV1Event.ActorRole.Name);
                    om.SenderId.Value.Should().Be(DataHubDetails.DataHubActorNumber.Value);
                    om.SenderRole.Name.Should().Be(ActorRole.MeteredDataAdministrator.Name);
                });

        var notifyMessageSent = await ThenNotifyOrchestrationInstanceWasSentOnServiceBusAsync(
            orchestrationInstanceId,
            RequestMeasurementsNotifyEventV1.OrchestrationInstanceEventName);
        notifyMessageSent.Should().BeTrue("Notify EnqueueActorMessagesCompleted service bus message should be sent");
    }

    [Fact]
    public async Task Given_EnqueueRejectedBrs025Message_When_MessageIsReceived_Then_RejectedMessageIsEnqueued()
    {
        // Arrange
        _fixture.EnsureAppHostUsesFeatureFlagValue(
        [
            new(FeatureFlagNames.PeekMeasurementMessages, true),
            new(FeatureFlagNames.UsePM28Enqueue, true)
        ]);

        const string receiverEnergySupplier = "1111111111111";
        var receiverEnergySupplierRole = ActorRole.EnergySupplier;

        var eventId = EventId.From(Guid.NewGuid());
        var orchestrationInstanceId = Guid.NewGuid();

        var requestMeasurementsRejectedV1 = CreateRequestMeasurementsRejectedV1(receiverEnergySupplier, receiverEnergySupplierRole);

        // Act
        await GivenEnqueueRejectedBrs025Message(requestMeasurementsRejectedV1, eventId, orchestrationInstanceId);

        // Assert
        using var assertionScope = new AssertionScope();
        await EnsureFunctionIsExecuted(nameof(EnqueueTrigger_Brs_025));
        var enqueuedOutgoingMessages = await EnsureOutgoingMessagesIsEnqueued(eventId);

        enqueuedOutgoingMessages.Should()
            .HaveCount(1)
            .And.AllSatisfy(
                (om) =>
                {
                    om.DocumentType.Should().Be(DocumentType.RejectRequestMeasurements);
                    om.BusinessReason.Should().Be(BusinessReason.PeriodicMetering.Name);
                    om.RelatedToMessageId!.Value.Value.Should().Be(requestMeasurementsRejectedV1.OriginalActorMessageId);
                    om.Receiver.Number.Value.Should().Be(requestMeasurementsRejectedV1.ActorNumber.Value);
                    om.Receiver.ActorRole.Name.Should().Be(requestMeasurementsRejectedV1.ActorRole.Name);
                    om.SenderId.Value.Should().Be(DataHubDetails.DataHubActorNumber.Value);
                    om.SenderRole.Name.Should().Be(ActorRole.MeteredDataAdministrator.Name);
                });

        var notifyMessageSent = await ThenNotifyOrchestrationInstanceWasSentOnServiceBusAsync(
            orchestrationInstanceId,
            RequestMeasurementsNotifyEventV1.OrchestrationInstanceEventName);
        notifyMessageSent.Should().BeTrue("Notify EnqueueActorMessagesCompleted service bus message should be sent");
    }

    private static RequestMeasurementsRejectV1 CreateRequestMeasurementsRejectedV1(
        string receiverEnergySupplier,
        ActorRole receiverEnergySupplierRole)
    {
        return new RequestMeasurementsRejectV1(
            OriginalActorMessageId: Guid.NewGuid().ToString(),
            OriginalTransactionId: Guid.NewGuid().ToString(),
            ActorNumber: ActorNumber.Create(receiverEnergySupplier).ToProcessManagerActorNumber(),
            ActorRole: receiverEnergySupplierRole.ToProcessManagerActorRole(),
            MeteringPointId: "1234567890123",
            ValidationErrors:
            [
                new(
                    Message:
                    "I forbindelse med anmodning om årssum kan der kun anmodes om data for forbrug og produktion/When"
                    + " requesting yearly amount then it is only possible to request for production and consumption",
                    ErrorCode: "D18"),

            ]);
    }

    private async Task EnsureFunctionIsExecuted(string functionName)
    {
        var functionResult = await _fixture.AppHostManager.WaitForFunctionToCompleteWithSucceededAsync(
            functionName: functionName);

        functionResult.Succeeded.Should().BeTrue("because the function should have been completed with success. Host log:\n{0}", functionResult.HostLog);
    }

    private async Task<List<OutgoingMessage>> EnsureOutgoingMessagesIsEnqueued(EventId eventId)
    {
        await using var dbContext = _fixture.DatabaseManager.CreateDbContext<ActorMessageQueueContext>();
        var enqueuedOutgoingMessages = await dbContext.OutgoingMessages
            .Where(om => om.EventId == eventId)
            .ToListAsync();
        return enqueuedOutgoingMessages;
    }

    private RequestMeasurementsAcceptedV1 CreateRequestMeasurementsAcceptedV1(
        Instant startDateTime,
        Instant endDateTime,
        PMValueTypes.Resolution resolution,
        string receiverEnergySupplier,
        ActorRole receiverEnergySupplierRole)
    {
        var enqueueMessagesData = new RequestMeasurementsAcceptedV1(
            OriginalActorMessageId: Guid.NewGuid().ToString(),
            OriginalTransactionId: Guid.NewGuid().ToString(),
            MeteringPointId: "1234567890123",
            MeteringPointType: PMValueTypes.MeteringPointType.Consumption,
            Measurements: new List<Measurement>
            {
                new(
                    StartDateTime: startDateTime.ToDateTimeOffset(),
                    EndDateTime: endDateTime.ToDateTimeOffset(),
                    Resolution: resolution,
                    MeasureUnit: PMValueTypes.MeasurementUnit.KilowattHour,
                    MeasurementPoints: GetMeasurementPoints(startDateTime.ToDateTimeOffset(), endDateTime.ToDateTimeOffset(), resolution)),
            },
            ActorNumber: ActorNumber.Create(receiverEnergySupplier).ToProcessManagerActorNumber(),
            ActorRole: receiverEnergySupplierRole.ToProcessManagerActorRole());
        return enqueueMessagesData;
    }

    private async Task GivenEnqueueAcceptedBrs025Message(
        RequestMeasurementsAcceptedV1 requestMeasurementsAccepted,
        EventId eventId,
        Guid orchestrationInstanceId)
    {
        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = Brs_025.Name,
            OrchestrationVersion = 1,
            OrchestrationStartedByActor = new EnqueueActorMessagesActorV1
            {
                ActorNumber = requestMeasurementsAccepted.ActorNumber.Value,
                ActorRole = requestMeasurementsAccepted.ActorRole.ToActorRoleV1(),
            },
            OrchestrationInstanceId = orchestrationInstanceId.ToString(),
        };
        enqueueActorMessages.SetData(requestMeasurementsAccepted);

        var serviceBusMessage = enqueueActorMessages.ToServiceBusMessage(
            subject: EnqueueActorMessagesV1.BuildServiceBusMessageSubject(enqueueActorMessages.OrchestrationName),
            idempotencyKey: eventId.Value);

        await _fixture.EdiTopicResource.SenderClient.SendMessageAsync(serviceBusMessage);
    }

    private async Task GivenEnqueueRejectedBrs025Message(
        RequestMeasurementsRejectV1 rejectedMessage,
        EventId eventId,
        Guid orchestrationInstanceId)
    {
        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = Brs_025.Name,
            OrchestrationVersion = 1,
            OrchestrationStartedByActor = new EnqueueActorMessagesActorV1
            {
                ActorNumber = rejectedMessage.ActorNumber.Value,
                ActorRole = rejectedMessage.ActorRole.ToActorRoleV1(),
            },
            OrchestrationInstanceId = orchestrationInstanceId.ToString(),
        };
        enqueueActorMessages.SetData(rejectedMessage);

        var serviceBusMessage = enqueueActorMessages.ToServiceBusMessage(
            subject: EnqueueActorMessagesV1.BuildServiceBusMessageSubject(enqueueActorMessages.OrchestrationName),
            idempotencyKey: eventId.Value);

        await _fixture.EdiTopicResource.SenderClient.SendMessageAsync(serviceBusMessage);
    }

    private IReadOnlyCollection<MeasurementPoint> GetMeasurementPoints(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        PMValueTypes.Resolution resolution)
    {
        var measurementPoints = new List<MeasurementPoint>();
        var currentTime = startDate;
        var position = 1;
        while (endDate > currentTime + GetResolutionDuration(resolution))
        {
            measurementPoints.Add(
                new MeasurementPoint(
                    Position: position,
                    EnergyQuantity: 100,
                    QuantityQuality: PMValueTypes.Quality.Calculated));

            currentTime += GetResolutionDuration(resolution);
        }

        return measurementPoints;
    }

    private TimeSpan GetResolutionDuration(PMValueTypes.Resolution resolution)
    {
         return resolution switch
        {
            var r when r == PMValueTypes.Resolution.QuarterHourly => TimeSpan.FromMinutes(15),
            var r when r == PMValueTypes.Resolution.Hourly => TimeSpan.FromHours(1),
            var r when r == PMValueTypes.Resolution.Daily => TimeSpan.FromDays(1),
            var r when r == PMValueTypes.Resolution.Monthly => throw new InvalidOperationException("Monthly resolution to duration is not supported, since a month is not a fixed duration."),
            _ => throw new ArgumentOutOfRangeException(nameof(resolution), resolution, "Unknown resolution."),
        };
    }
}
