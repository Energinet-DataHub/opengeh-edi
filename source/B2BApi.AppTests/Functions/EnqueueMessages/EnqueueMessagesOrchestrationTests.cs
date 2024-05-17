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

using Energinet.DataHub.Core.FunctionApp.TestCommon.ServiceBus.ListenerMock;
using Energinet.DataHub.Core.FunctionApp.TestCommon.ServiceBus.ResourceProvider;
using Energinet.DataHub.EDI.B2BApi.AppTests.DurableTask;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures;
using FluentAssertions;
using FluentAssertions.Execution;
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

    /// <summary>
    /// Verifies that:
    ///  - The orchestration can complete a full run.
    ///  - Every activity is executed once and in correct order.
    ///  - A service bus message is sent as expected.
    /// </summary>
    [Fact]
    public async Task Given_CalculationCompletedEvent_When_Received_Then_OrchestrationCompletesWithExpectedServiceBusMessage()
    {
        // Arrange
        var calculationOrchestrationId = Guid.NewGuid();
        var calculationCompletedEventMessage = new Azure.Messaging.ServiceBus.ServiceBusMessage();

        // Act
        var beforeOrchestrationCreated = DateTime.UtcNow;
        await Fixture.TopicResource.SenderClient.SendMessageAsync(calculationCompletedEventMessage);

        // Assert
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
            "SendEnqueueMessagesCompletedActivity",
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
                return msg.Subject == calculationOrchestrationId.ToString();
            })
            .VerifyCountAsync(1);

        var wait = verifyServiceBusMessages.Wait(TimeSpan.FromSeconds(10));
        wait.Should().BeTrue("We did not receive the expected message on the ServiceBus");
    }
}
