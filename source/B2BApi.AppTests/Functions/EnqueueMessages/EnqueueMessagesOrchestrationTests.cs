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
using Energinet.DataHub.EDI.B2BApi.AppTests.DurableTask;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EnergySupplying.RequestResponse.IntegrationEvents;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Energinet.DataHub.Wholesale.Events.Infrastructure.IntegrationEvents;
using FluentAssertions;
using FluentAssertions.Execution;
using Google.Protobuf;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Options;
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

    public async Task InitializeAsync()
    {
        Fixture.AppHostManager.ClearHostLog();

        // Clear mappings etc. before each test
        Fixture.ServiceBusListenerMock.ResetMessageHandlersAndReceivedMessages();

        // Ensure that databricks does not contain data, unless the test explicit adds it
        await Fixture.DatabricksSchemaManager.DropSchemaAsync();
    }

    public async Task DisposeAsync()
    {
        await Fixture.DatabricksSchemaManager.DropSchemaAsync();
        Fixture.SetTestOutputHelper(null!);
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

        // create databricks data
        var calculationId = await AddDatabricksData();

        var calculationOrchestrationId = Guid.NewGuid().ToString();
        var calculationCompletedEventMessage = CreateCalculationCompletedEventMessage(calculationOrchestrationId, calculationId.ToString());

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
            "EnqueueMessagesActivity",
            "SendMessagesEnqueuedActivity",
            null,
        ]);

        // => Verify that the durable function completed successfully
        var last = completeOrchestrationStatus.History.Last();
        last.Value<string>("EventType").Should().Be("ExecutionCompleted");
        last.Value<string>("Result").Should().Be("Success");

        // => Verify that the expected message was sent on the ServiceBus
        var verifyServiceBusMessages = await Fixture.ServiceBusListenerMock
            .When(msg =>
            {
                if (msg.Subject != ActorMessagesEnqueuedV1.EventName)
                {
                    return false;
                }

                var parsedEvent = ActorMessagesEnqueuedV1.Parser.ParseFrom(msg.Body);

                var matchingOrchestrationId = parsedEvent.OrchestrationInstanceId == calculationOrchestrationId;
                var matchingCalculationId = parsedEvent.CalculationId == calculationId.ToString();
                var isSuccessful = parsedEvent.Success;

                return matchingOrchestrationId && matchingCalculationId && isSuccessful;
            })
            .VerifyCountAsync(1);

        var wait = verifyServiceBusMessages.Wait(TimeSpan.FromSeconds(10));
        wait.Should().BeTrue("We did not receive the expected message on the ServiceBus");
    }

    /// <summary>
    /// Verifies that:
    ///  - If databricks has no data, the orchestration completes with a failed service bus message.
    /// </summary>
    [Fact]
    public async Task Given_FeatureFlagIsEnabledAndCalculationOrchestrationId_When_CalculationCompletedEventIsHandledAndDatabricksHasNoData_Then_OrchestrationCompletesWithFailedServiceBusMessage()
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
        await Fixture.DurableClient.WaitForInstanceCompletedAsync(
            orchestrationStatus.InstanceId,
            TimeSpan.FromMinutes(1));

        // => Expect history
        using var assertionScope = new AssertionScope();

        // => Verify that the expected message was sent on the ServiceBus
        var verifyServiceBusMessages = await Fixture.ServiceBusListenerMock
            .When(msg =>
            {
                if (msg.Subject != ActorMessagesEnqueuedV1.EventName)
                {
                    return false;
                }

                var parsedEvent = ActorMessagesEnqueuedV1.Parser.ParseFrom(msg.Body);

                var matchingOrchestrationId = parsedEvent.OrchestrationInstanceId == calculationOrchestrationId;
                var isFailed = parsedEvent.Success == false;

                return matchingOrchestrationId && isFailed;
            })
            .VerifyCountAsync(1);

        var wait = verifyServiceBusMessages.Wait(TimeSpan.FromSeconds(10));
        wait.Should().BeTrue("We did not receive the expected message on the ServiceBus");
    }

    private static ServiceBusMessage CreateCalculationCompletedEventMessage(string calculationOrchestrationId, string? calculationId = null)
    {
        var calculationCompletedEvent = new CalculationCompletedV1
        {
            InstanceId = calculationOrchestrationId,
            CalculationId = calculationId ?? Guid.NewGuid().ToString(),
            CalculationType = CalculationCompletedV1.Types.CalculationType.BalanceFixing,
            CalculationVersion = 1,
        };

        return CreateServiceBusMessage(eventId: Guid.NewGuid(), calculationCompletedEvent);
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

    private async Task<Guid> AddDatabricksData()
    {
        var calculationId = Guid.Parse("e7a26e65-be5e-4db0-ba0e-a6bb4ae2ef3d");
        await Fixture.DatabricksSchemaManager.CreateSchemaAsync();

        var ediOptions = Options.Create(
            new EdiDatabricksOptions { DatabaseName = Fixture.DatabricksSchemaManager.SchemaName });

        var viewQuery = new EnergyResultPerGridAreaQuery(
            ediOptions,
            calculationId);
        await Fixture.DatabricksSchemaManager.CreateTableAsync(viewQuery);

        var testFilename = "balance_fixing_01-11-2022_01-12-2022_ga_543.csv";
        var testFilePath = Path.Combine("TestData", testFilename);
        await Fixture.DatabricksSchemaManager.InsertFromCsvFileAsync(viewQuery, testFilePath);

        return calculationId;
    }
}
