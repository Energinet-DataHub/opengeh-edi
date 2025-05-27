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
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Given_EnqueueAcceptedBrs024Message_When_MessageIsReceived_Then_AcceptedMessagesIsEnqueued()
    {
        _fixture.EnsureAppHostUsesFeatureFlagValue(
        [
            new(FeatureFlagNames.PeekMeasurementMessages, true)
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
        var enqueueMessagesData = new RequestMeasurementsAcceptedV1(
            OriginalActorMessageId: Guid.NewGuid().ToString(),
            MeteringPointId: "1234567890123",
            MeteringPointType: PMValueTypes.MeteringPointType.Consumption,
            ProductNumber: "test-product-number",
            RegistrationDateTime: startDateTime.ToDateTimeOffset(),
            StartDateTime: startDateTime.ToDateTimeOffset(),
            EndDateTime: endDateTime.ToDateTimeOffset(),
            ActorNumber: ActorNumber.Create(receiverEnergySupplier).ToProcessManagerActorNumber(),
            ActorRole: receiverEnergySupplierRole.ToProcessManagerActorRole(),
            Resolution: resolution,
            MeasureUnit: PMValueTypes.MeasurementUnit.KilowattHour,
            Measurements: GetMeasurements(startDateTime, endDateTime, resolution),
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
                    om.BusinessReason.Should().Be(BusinessReason.PeriodicMetering.Name);
                    om.RelatedToMessageId.Should().NotBeNull();
                    om.RelatedToMessageId!.Value.Value.Should().Be(enqueueMessagesData.OriginalActorMessageId);
                    om.Receiver.Number.Value.Should().Be(enqueueMessagesData.ActorNumber.Value);
                    om.Receiver.ActorRole.Name.Should().Be(enqueueMessagesData.ActorRole.Name);
                });

        var notifyMessageSent = await ThenNotifyOrchestrationInstanceWasSentOnServiceBusAsync(
            orchestrationInstanceId,
            RequestMeasurementsNotifyEventV1.OrchestrationInstanceEventName);
        notifyMessageSent.Should().BeTrue("Notify EnqueueActorMessagesCompleted service bus message should be sent");
    }

    private async Task GivenEnqueueAcceptedBrs024Message(EnqueueActorMessagesV1 enqueueActorMessages, EventId eventId)
    {
        var serviceBusMessage = enqueueActorMessages.ToServiceBusMessage(
            subject: EnqueueActorMessagesV1.BuildServiceBusMessageSubject(enqueueActorMessages.OrchestrationName),
            idempotencyKey: eventId.Value);

        await _fixture.EdiTopicResource.SenderClient.SendMessageAsync(serviceBusMessage);
    }

    private IReadOnlyCollection<AcceptedMeteredData> GetMeasurements(
        Instant startDateTime,
        Instant endDateTime,
        PMValueTypes.Resolution resolution)
    {
        var measurements = new List<AcceptedMeteredData>();
        var interval = resolution switch
        {
            var res when res == PMValueTypes.Resolution.QuarterHourly => Duration.FromMinutes(15),
            var res when res == PMValueTypes.Resolution.Hourly => Duration.FromHours(1),
            var res when res == PMValueTypes.Resolution.Daily => Duration.FromDays(1),
            _ => throw new ArgumentOutOfRangeException(nameof(resolution), "Unsupported resolution"),
        };

        var position = 1;
        for (var timestamp = startDateTime; timestamp < endDateTime; timestamp += interval)
        {
            measurements.Add(
                new AcceptedMeteredData(
                    Position: position,
                    EnergyQuantity: GenerateRandomMeasurementValue(),
                    QuantityQuality: PMValueTypes.Quality.AsProvided));
            position++;
        }

        return measurements;
    }

    private decimal GenerateRandomMeasurementValue()
    {
        // Example: Generate a random value for demonstration purposes
        var random = new Random();
        return (decimal)(random.Next(0, 10000) / 100.0); // Random decimal value between 0.00 and 100.00
    }
}
