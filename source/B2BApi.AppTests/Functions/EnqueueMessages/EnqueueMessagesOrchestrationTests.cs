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

using Azure.Messaging.ServiceBus;
using Energinet.DataHub.Core.FunctionApp.TestCommon.ServiceBus.ListenerMock;
using Energinet.DataHub.Core.FunctionApp.TestCommon.ServiceBus.ResourceProvider;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.EDI.B2BApi.AppTests.DurableTask;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures;
using Energinet.DataHub.EnergySupplying.RequestResponse.IntegrationEvents;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Energinet.DataHub.Wholesale.Events.Infrastructure.IntegrationEvents;
using FluentAssertions;
using FluentAssertions.Execution;
using Google.Protobuf;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.B2BApi.AppTests.Functions.EnqueueMessages;

[Collection(nameof(B2BApiAppCollectionFixture))]
public class EnqueueMessagesOrchestrationTests : IAsyncLifetime
{
    public EnqueueMessagesOrchestrationTests(
        B2BApiAppFixture fixture,
        ITestOutputHelper testOutputHelper)
    {
        Fixture = fixture;
        Fixture.SetTestOutputHelper(testOutputHelper);
    }

    private B2BApiAppFixture Fixture { get; }

    public Task InitializeAsync()
    {
        Fixture.AppHostManager.ClearHostLog();

        // Clear mappings etc. before each test
        Fixture.ServiceBusListenerMock.ResetMessageHandlersAndReceivedMessages();

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        Fixture.SetTestOutputHelper(null!);

        return Task.CompletedTask;
    }

    [Fact]
    public async Task Given_FeatureFlagIsDisabled_When_CalculationCompletedEventIsSent_Then_OrchestrationIsNeverStarted()
    {
        // Arrange
        Fixture.EnsureAppHostUsesFeatureFlagValue(enableCalculationCompletedEvent: false);

        var calculationOrchestrationId = Guid.NewGuid().ToString();
        var calculationCompletedEventMessage = CreateCalculationCompletedEventMessage(calculationOrchestrationId);

        // Act
        var beforeOrchestrationCreated = DateTime.UtcNow;
        await Fixture.TopicResource.SenderClient.SendMessageAsync(calculationCompletedEventMessage);

        // Assert
        await Task.Delay(TimeSpan.FromSeconds(30));

        var filter = new OrchestrationStatusQueryCondition()
        {
            CreatedTimeFrom = beforeOrchestrationCreated,
            RuntimeStatus =
            [
                OrchestrationRuntimeStatus.Pending,
                OrchestrationRuntimeStatus.Running,
                OrchestrationRuntimeStatus.Completed,
            ],
        };
        var queryResult = await Fixture.DurableClient.ListInstancesAsync(filter, CancellationToken.None);

        var actualOrchestrationStatus = queryResult.DurableOrchestrationState.FirstOrDefault();
        actualOrchestrationStatus.Should().BeNull();
    }

    /// <summary>
    /// Verifies that:
    ///  - The orchestration can complete a full run.
    ///  - Every activity is executed once and in correct order.
    ///  - A service bus message is sent as expected.
    /// </summary>
    [Fact]
    public async Task Given_FeatureFlagIsEnabledAndCalculationOrchestrationId_When_CalculationCompletedEventIsHandled_Then_OrchestrationCompletesWithExpectedServiceBusMessage()
    {
        // Arrange
        Fixture.EnsureAppHostUsesFeatureFlagValue(enableCalculationCompletedEvent: true);

        var calculationOrchestrationId = Guid.NewGuid().ToString();
        var calculationCompletedEventMessage = CreateCalculationCompletedEventMessage(calculationOrchestrationId);

        // Act
        var beforeOrchestrationCreated = DateTime.UtcNow;
        await Fixture.TopicResource.SenderClient.SendMessageAsync(calculationCompletedEventMessage);

        // Assert
        await Task.Delay(TimeSpan.FromSeconds(30));

        // => Verify expected behaviour by searching the orchestration history
        var orchestrationStatus = await Fixture.DurableClient.FindOrchestationStatusAsync(createdTimeFrom: beforeOrchestrationCreated);

        // => Wait for completion, this should be fairly quick
        var completeOrchestrationStatus = await Fixture.DurableClient.WaitForInstanceCompletedAsync(
            orchestrationStatus.InstanceId,
            TimeSpan.FromMinutes(1));

        // => Expect history
        using var assertionScope = new AssertionScope();

        var activities = completeOrchestrationStatus.History
            .OrderBy(item => item["Timestamp"])
            .Select(item => item.Value<string>("FunctionName"));

        activities.Should().NotBeNull().And.Equal(
        [
            "EnqueueMessagesOrchestration",
            "SendMessagesEnqueuedActivity",
            null
        ]);

        // => Verify that the durable function completed successfully
        var last = completeOrchestrationStatus.History.Last();
        last.Value<string>("EventType").Should().Be("ExecutionCompleted");
        last.Value<string>("Result").Should().Be("Success");

        // => Verify that the expected message was sent on the ServiceBus
        var verifyServiceBusMessages = await Fixture.ServiceBusListenerMock
            .When(msg =>
            {
                if (msg.Subject != MessagesEnqueuedV1.EventName)
                {
                    return false;
                }

                var parsedEvent = MessagesEnqueuedV1.Parser.ParseFrom(msg.Body);
                return parsedEvent.OrchestrationInstanceId == calculationOrchestrationId;
            })
            .VerifyCountAsync(1);

        var wait = verifyServiceBusMessages.Wait(TimeSpan.FromSeconds(10));
        wait.Should().BeTrue("We did not receive the expected message on the ServiceBus");
    }

    private static ServiceBusMessage CreateCalculationCompletedEventMessage(string calculationOrchestrationId)
    {
        var calcuationCompletedEvent = new CalculationCompletedV1
        {
            InstanceId = calculationOrchestrationId,
            CalculationId = Guid.NewGuid().ToString(),
            CalculationType = CalculationCompletedV1.Types.CalculationType.BalanceFixing,
            CalculationVersion = 1,
        };

        return CreateServiceBusMessage(eventId: Guid.NewGuid(), calcuationCompletedEvent);
    }

    private static ServiceBusMessage CreateServiceBusMessage(Guid eventId, IEventMessage eventMessage)
    {
        var serviceBusMessage = new ServiceBusMessage
        {
            Body = new BinaryData(eventMessage.ToByteArray()),
            Subject = eventMessage.EventName,
            MessageId = eventId.ToString(),
        };

        serviceBusMessage.ApplicationProperties.Add("EventMinorVersion", eventMessage.EventMinorVersion);

        return serviceBusMessage;
    }
}
