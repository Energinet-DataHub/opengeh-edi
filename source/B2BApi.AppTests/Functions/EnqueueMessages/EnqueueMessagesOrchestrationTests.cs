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
using Energinet.DataHub.Core.FunctionApp.TestCommon.Databricks;
using Energinet.DataHub.Core.FunctionApp.TestCommon.ServiceBus.ListenerMock;
using Energinet.DataHub.Core.TestCommon;
using Energinet.DataHub.EDI.B2BApi.AppTests.DurableTask;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.Behaviours.IntegrationEvents.TestData;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.SqlStatements;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Queries;
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

    public Task InitializeAsync()
    {
        Fixture.AppHostManager.ClearHostLog();

        // Clear mappings etc. before each test
        Fixture.ServiceBusListenerMock.ResetMessageHandlersAndReceivedMessages();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (Fixture.DatabricksSchemaManager.SchemaExists)
            await Fixture.DatabricksSchemaManager.DropSchemaAsync();

        Fixture.SetTestOutputHelper(null!);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(CalculationCompletedV1.Types.CalculationType.BalanceFixing)]
    [InlineData(CalculationCompletedV1.Types.CalculationType.WholesaleFixing)]
    public async Task Given_FeatureFlagIsDisabledForCalculationType_When_CalculationCompletedEventIsSent_Then_OrchestrationIsNeverStarted(CalculationCompletedV1.Types.CalculationType? calculationTypeToTest)
    {
        // Arrange
        // => If calculationTypeToTest is null, then we test disabling the UseCalculationCompletedEvent feature flag
        // => If calculationTypeToTest is BalanceFixing, then we test disabling the balance fixing feature flag
        // => If calculationTypeToTest is WholesaleFixing, then we test disabling the wholesale fixing feature flag
        Fixture.EnsureAppHostUsesFeatureFlagValue(
            enableCalculationCompletedEvent: calculationTypeToTest != null,
            enableCalculationCompletedEventForBalanceFixing: calculationTypeToTest != CalculationCompletedV1.Types.CalculationType.BalanceFixing,
            enableCalculationCompletedEventForWholesaleFixing: calculationTypeToTest != CalculationCompletedV1.Types.CalculationType.WholesaleFixing);

        var calculationOrchestrationId = Guid.NewGuid().ToString();
        var calculationCompletedEventMessage = CreateCalculationCompletedEventMessage(
            calculationOrchestrationId,
            calculationTypeToTest);

        // Act
        var beforeOrchestrationCreated = DateTime.UtcNow;
        await Fixture.TopicResource.SenderClient.SendMessageAsync(calculationCompletedEventMessage);

        // Assert
        var act = async () => await Fixture.DurableClient.WaitForOrchestationStatusAsync(createdTimeFrom: beforeOrchestrationCreated);
        await act.Should()
            .ThrowAsync<Exception>()
            .WithMessage("Orchestration did not start within configured wait time*");
    }

    /// <summary>
    /// Verifies that:
    ///  - The orchestration can complete a full run.
    ///  - Every activity is executed once. We cannot be sure in which order, because we use fan-out/fan-in.
    ///  - A service bus message is sent as expected.
    /// </summary>
    /// <remarks>
    /// Feature flags are enabled for all calculation types to ensure activities are executed.
    /// </remarks>
    [Fact]
    public async Task Given_CalculationOrchestrationId_When_CalculationCompletedEventForBalanceFixingIsHandled_Then_OrchestrationCompletesWithExpectedServiceBusMessage()
    {
        // Arrange
        EnableEnqueueMessagesOrchestration();

        var perGridAreaDataDescription = new EnergyResultPerGridAreaDescription();
        var perBrpGridAreaDataDescription = new EnergyResultPerBrpGridAreaDescription();
        var perBrpAndEsGridAreaDataDescription = new EnergyResultPerEnergySupplierBrpGridAreaDescription();
        var forAmountPerChargeDescription = new WholesaleResultForAmountPerChargeDescription();
        var forMonthlyAmountPerChargeDescription = new WholesaleResultForMonthlyAmountPerChargeDescription();
        var forTotalAmountDescription = new WholesaleResultForTotalAmountDescription();
        var calculationId = await ClearAndAddDatabricksData(
            perGridAreaDataDescription,
            perBrpGridAreaDataDescription,
            perBrpAndEsGridAreaDataDescription,
            forAmountPerChargeDescription,
            forMonthlyAmountPerChargeDescription,
            forTotalAmountDescription);

        var calculationOrchestrationId = Guid.NewGuid().ToString();
        var calculationCompletedEventMessage = CreateCalculationCompletedEventMessage(
            calculationOrchestrationId,
            CalculationCompletedV1.Types.CalculationType.BalanceFixing,
            calculationId.ToString());

        // Act
        var beforeOrchestrationCreated = DateTime.UtcNow;
        await Fixture.TopicResource.SenderClient.SendMessageAsync(calculationCompletedEventMessage);

        // Assert
        // => Verify expected behaviour by searching the orchestration history
        var actualOrchestrationStatus = await Fixture.DurableClient.WaitForOrchestationStatusAsync(createdTimeFrom: beforeOrchestrationCreated);

        // => Wait for completion
        var completeOrchestrationStatus = await Fixture.DurableClient.WaitForInstanceCompletedAsync(
            actualOrchestrationStatus.InstanceId,
            TimeSpan.FromMinutes(5));

        // => Expect history
        using var assertionScope = new AssertionScope();

        var activities = completeOrchestrationStatus.History
            .OrderBy(item => item["Timestamp"])
            .Select(item =>
                (item.Value<string>("FunctionName"), item.Value<string>("Result")));

        activities.Should().NotBeNull().And.Contain(
        [
            ("EnqueueMessagesOrchestration", null),
            ("EnqueueEnergyResultsForGridAreaOwnersActivity", perGridAreaDataDescription.ExpectedCalculationResultsCount.ToString()),
            ("EnqueueEnergyResultsForBalanceResponsiblesActivity", perBrpGridAreaDataDescription.ExpectedCalculationResultsCount.ToString()),
            ("EnqueueEnergyResultsForBalanceResponsiblesAndEnergySuppliersActivity", perBrpAndEsGridAreaDataDescription.ExpectedCalculationResultsCount.ToString()),
            ("EnqueueWholesaleResultsForAmountPerChargesActivity", "0"),
            ("EnqueueWholesaleResultsForMonthlyAmountPerChargesActivity", "0"),
            ("EnqueueWholesaleResultsForTotalAmountsActivity", "0"),
            ("SendActorMessagesEnqueuedActivity", null),
            (null, "Success"),
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
        wait.Should().BeTrue("ActorMessagesEnqueuedV1 service bus message should be sent");
    }

    /// <summary>
    /// Verifies that:
    ///  - The orchestration can complete a full run.
    ///  - Every activity is executed once. We cannot be sure in which order, because we use fan-out/fan-in.
    ///  - A service bus message is sent as expected.
    /// </summary>
    /// <remarks>
    /// Feature flags are enabled for all calculation types to ensure activities are executed.
    /// </remarks>
    [Fact]
    public async Task Given_CalculationOrchestrationId_When_CalculationCompletedEventForWholesaleFixingIsHandled_Then_OrchestrationCompletesWithExpectedServiceBusMessage()
    {
        // Arrange
        EnableEnqueueMessagesOrchestration();

        var perGridAreaDataDescription = new EnergyResultPerGridAreaDescription();
        var perBrpGridAreaDataDescription = new EnergyResultPerBrpGridAreaDescription();
        var perBrpAndEsGridAreaDataDescription = new EnergyResultPerEnergySupplierBrpGridAreaDescription();
        var forAmountPerChargeDescription = new WholesaleResultForAmountPerChargeDescription();
        var forMonthlyAmountPerChargeDescription = new WholesaleResultForMonthlyAmountPerChargeDescription();
        var forTotalAmountDescription = new WholesaleResultForTotalAmountDescription();
        await ClearAndAddDatabricksData(
            perGridAreaDataDescription,
            perBrpGridAreaDataDescription,
            perBrpAndEsGridAreaDataDescription,
            forAmountPerChargeDescription,
            forMonthlyAmountPerChargeDescription,
            forTotalAmountDescription);
        var calculationId = forAmountPerChargeDescription.CalculationId;

        var calculationOrchestrationId = Guid.NewGuid().ToString();
        var calculationCompletedEventMessage = CreateCalculationCompletedEventMessage(
            calculationOrchestrationId,
            CalculationCompletedV1.Types.CalculationType.WholesaleFixing,
            calculationId.ToString());

        // Act
        var beforeOrchestrationCreated = DateTime.UtcNow;
        await Fixture.TopicResource.SenderClient.SendMessageAsync(calculationCompletedEventMessage);

        // Assert
        // => Verify expected behaviour by searching the orchestration history
        var actualOrchestrationStatus = await Fixture.DurableClient.WaitForOrchestationStatusAsync(createdTimeFrom: beforeOrchestrationCreated);

        // => Wait for completion
        var completeOrchestrationStatus = await Fixture.DurableClient.WaitForInstanceCompletedAsync(
            actualOrchestrationStatus.InstanceId,
            TimeSpan.FromMinutes(5));

        // => Expect history
        using var assertionScope = new AssertionScope();

        var activities = completeOrchestrationStatus.History
            .OrderBy(item => item["Timestamp"])
            .Select(item =>
                (item.Value<string>("FunctionName"), item.Value<string>("Result")));

        activities.Should().NotBeNull().And.Contain(
        [
            ("EnqueueMessagesOrchestration", null),
            ("EnqueueEnergyResultsForGridAreaOwnersActivity", "0"),
            ("EnqueueEnergyResultsForBalanceResponsiblesActivity", "0"),
            ("EnqueueEnergyResultsForBalanceResponsiblesAndEnergySuppliersActivity", "0"),
            ("EnqueueWholesaleResultsForAmountPerChargesActivity", forAmountPerChargeDescription.ExpectedCalculationResultsCount.ToString()),
            ("EnqueueWholesaleResultsForMonthlyAmountPerChargesActivity", forMonthlyAmountPerChargeDescription.ExpectedCalculationResultsCount.ToString()),
            ("EnqueueWholesaleResultsForTotalAmountsActivity", forTotalAmountDescription.ExpectedCalculationResultsCount.ToString()),
            ("SendActorMessagesEnqueuedActivity", null),
            (null, "Success"),
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
        wait.Should().BeTrue("ActorMessagesEnqueuedV1 service bus message should be sent");
    }

    /// <summary>
    /// Verifies that:
    /// - If databricks has no data for the CalculationId and CalculationType, then orchestration runs "forever" (because of retry policies).
    /// </summary>
    [Theory]
    [InlineData(CalculationCompletedV1.Types.CalculationType.BalanceFixing)]
    [InlineData(CalculationCompletedV1.Types.CalculationType.WholesaleFixing)]
    public async Task Given_DatabricksHasNoData_When_CalculationCompletedEventIsHandled_Then_OrchestrationIsStartedButActivitiesWillFailAndBeRetriedForever(CalculationCompletedV1.Types.CalculationType calculationTypeToTest)
    {
        // Arrange
        EnableEnqueueMessagesOrchestration();

        var calculationId = Guid.NewGuid().ToString();
        var calculationOrchestrationId = Guid.NewGuid().ToString();
        var calculationCompletedEventMessage = CreateCalculationCompletedEventMessage(
            calculationOrchestrationId,
            calculationTypeToTest,
            calculationId);

        var expectedHistory = new List<(string?, string?)>
        {
            ("EnqueueEnergyResultsForGridAreaOwnersActivity", null),
            ("EnqueueEnergyResultsForBalanceResponsiblesActivity", null),
            ("EnqueueEnergyResultsForBalanceResponsiblesAndEnergySuppliersActivity", null),
            ("EnqueueWholesaleResultsForAmountPerChargesActivity", null),
            ("EnqueueWholesaleResultsForMonthlyAmountPerChargesActivity", null),
            ("EnqueueWholesaleResultsForTotalAmountsActivity", null),
        };

        // Act
        var beforeOrchestrationCreated = DateTime.UtcNow;
        await Fixture.TopicResource.SenderClient.SendMessageAsync(calculationCompletedEventMessage);

        // Assert
        // => Verify expected behaviour by searching the orchestration history
        var actualOrchestrationStatus = await Fixture.DurableClient.WaitForOrchestationStatusAsync(createdTimeFrom: beforeOrchestrationCreated);

        // => Wait for running and expected history
        var isExpected = await Awaiter.TryWaitUntilConditionAsync(
            async () =>
            {
                var orchestrationStatus = await Fixture.DurableClient.GetStatusAsync(actualOrchestrationStatus.InstanceId, showHistory: true);

                if (orchestrationStatus.RuntimeStatus != OrchestrationRuntimeStatus.Running)
                    return false;

                var activities = orchestrationStatus.History
                    .OrderBy(item => item["Timestamp"])
                    .Select(item =>
                        (item.Value<string>("FunctionName"), item.Value<string>("Result")));

                var containsExpectedHistory = expectedHistory.Intersect(activities).Count() == expectedHistory.Count();
                return containsExpectedHistory;
            },
            TimeSpan.FromSeconds(30),
            delay: TimeSpan.FromSeconds(5));

        isExpected.Should().BeTrue("because we expect the actual history to contain the expected history");
    }

    [Fact]
    public async Task Given_WholesaleResultsContainsAnInvalidRow_When_CalculationCompletedEventForWholesaleFixing_Then_EnqueueAllValidMessages()
    {
        // Arrange
        EnableEnqueueMessagesOrchestration();

        var forAmountPerChargeDescription = new WholesaleResultForAmountPerChargeDescription();
        var forMonthlyAmountPerChargeDescription = new WholesaleResultForMonthlyAmountPerChargeDescription();
        var forTotalAmountDescription = new WholesaleResultForTotalAmountDescription();
        await ClearAndAddInvalidDatabricksData(
            forAmountPerChargeDescription,
            forMonthlyAmountPerChargeDescription,
            forTotalAmountDescription);
        var wholesaleCalculationId = forAmountPerChargeDescription.CalculationId;

        var wholesaleCalculationOrchestrationId = Guid.NewGuid().ToString();
        var wholesaleCalculationCompletedEventMessage = CreateCalculationCompletedEventMessage(
            wholesaleCalculationOrchestrationId,
            CalculationCompletedV1.Types.CalculationType.WholesaleFixing,
            wholesaleCalculationId.ToString());

        // Act
        var beforeOrchestrationCreated = DateTime.UtcNow;
        await Fixture.TopicResource.SenderClient.SendMessageAsync(wholesaleCalculationCompletedEventMessage);
        var actualWholesaleOrchestrationStatus = await Fixture.DurableClient.WaitForOrchestationStatusAsync(createdTimeFrom: beforeOrchestrationCreated);

        // Assert
        using var assertionScope = new AssertionScope();
        var wholesaleEventId = actualWholesaleOrchestrationStatus.Input["EventId"];

        var expectedHistory = new List<(string?, string?)>
        {
            ("EnqueueWholesaleResultsForAmountPerChargesActivity", $"Enqueue messages activity failed. CalculationId='{wholesaleCalculationId}' EventId='{wholesaleEventId}' NumberOfFailedResults='1' NumberOfHandledResults='{forAmountPerChargeDescription.ExpectedCalculationResultsCount - 1}'"),
            ("EnqueueWholesaleResultsForMonthlyAmountPerChargesActivity", $"Enqueue messages activity failed. CalculationId='{wholesaleCalculationId}' EventId='{wholesaleEventId}' NumberOfFailedResults='1' NumberOfHandledResults='{forMonthlyAmountPerChargeDescription.ExpectedCalculationResultsCount - 1}'"),
            ("EnqueueWholesaleResultsForTotalAmountsActivity", $"Enqueue messages activity failed. CalculationId='{wholesaleCalculationId}' EventId='{wholesaleEventId}' NumberOfFailedResults='1' NumberOfHandledResults='{forTotalAmountDescription.ExpectedCalculationResultsCount - 1}'"),
        };

        // => Wait for running and expected history
        var isExpected = await Awaiter.TryWaitUntilConditionAsync(
            async () =>
            {
                var orchestrationStatus = await Fixture.DurableClient.GetStatusAsync(actualWholesaleOrchestrationStatus.InstanceId, showHistory: true);

                if (orchestrationStatus.RuntimeStatus != OrchestrationRuntimeStatus.Running)
                    return false;

                var activities = orchestrationStatus.History
                    .OrderBy(item => item["Timestamp"])
                    .Where(item => item.HasValues)
                    .Select(item =>
                        (item.Value<string>("FunctionName"), (string?)item.Value<dynamic>("FailureDetails")?.ErrorMessage))
                    .ToList();

                var containsExpectedHistory = expectedHistory.Intersect(activities).Count() == expectedHistory.Count();
                return containsExpectedHistory;
            },
            TimeSpan.FromSeconds(30),
            delay: TimeSpan.FromSeconds(5));

        isExpected.Should().BeTrue("because we expect the actual history to contain the expected history");
    }

    [Fact]
    public async Task Given_EnergyResultsContainsAnInvalidRow_When_CalculationCompletedEventForBalanceFixing_Then_EnqueueAllValidMessages()
    {
        // Arrange
        EnableEnqueueMessagesOrchestration();

        var perGridAreaDataDescription = new EnergyResultPerGridAreaDescription();
        var perBrpGridAreaDataDescription = new EnergyResultPerBrpGridAreaDescription();
        var perBrpAndEsGridAreaDataDescription = new EnergyResultPerEnergySupplierBrpGridAreaDescription();
        await ClearAndAddInvalidDatabricksData(
            perGridAreaDataDescription,
            perBrpGridAreaDataDescription,
            perBrpAndEsGridAreaDataDescription);
        var energyCalculationId = perBrpAndEsGridAreaDataDescription.CalculationId;

        var energyCalculationOrchestrationId = Guid.NewGuid().ToString();
        var energyCalculationCompletedEventMessage = CreateCalculationCompletedEventMessage(
            energyCalculationOrchestrationId,
            CalculationCompletedV1.Types.CalculationType.BalanceFixing,
            energyCalculationId.ToString());

        // Act
        var beforeOrchestrationCreated = DateTime.UtcNow;
        await Fixture.TopicResource.SenderClient.SendMessageAsync(energyCalculationCompletedEventMessage);
        var actualEnergyOrchestrationStatus = await Fixture.DurableClient.WaitForOrchestationStatusAsync(createdTimeFrom: beforeOrchestrationCreated);

        // Assert
        using var assertionScope = new AssertionScope();
        var energyEventId = actualEnergyOrchestrationStatus.Input["EventId"];

        var expectedHistory = new List<(string?, string?)>
        {
            ("EnqueueEnergyResultsForGridAreaOwnersActivity", $"Enqueue messages activity failed. CalculationId='{energyCalculationId}' EventId='{energyEventId}' NumberOfFailedResults='1' NumberOfHandledResults='{perGridAreaDataDescription.ExpectedCalculationResultsCount - 1}'"),
            ("EnqueueEnergyResultsForBalanceResponsiblesActivity", $"Enqueue messages activity failed. CalculationId='{energyCalculationId}' EventId='{energyEventId}' NumberOfFailedResults='1' NumberOfHandledResults='{perBrpGridAreaDataDescription.ExpectedCalculationResultsCount - 1}'"),
            ("EnqueueEnergyResultsForBalanceResponsiblesAndEnergySuppliersActivity", $"Enqueue messages activity failed. CalculationId='{energyCalculationId}' EventId='{energyEventId}' NumberOfFailedResults='1' NumberOfHandledResults='{perBrpAndEsGridAreaDataDescription.ExpectedCalculationResultsCount - 1}'"),
        };

        // => Wait for running and expected history
        var isExpected = await Awaiter.TryWaitUntilConditionAsync(
            async () =>
            {
                var orchestrationStatus = await Fixture.DurableClient.GetStatusAsync(actualEnergyOrchestrationStatus.InstanceId, showHistory: true);

                if (orchestrationStatus.RuntimeStatus != OrchestrationRuntimeStatus.Running)
                    return false;

                var activities = orchestrationStatus.History
                    .OrderBy(item => item["Timestamp"])
                    .Where(item => item.HasValues)
                    .Select(item =>
                        (item.Value<string>("FunctionName"), (string?)item.Value<dynamic>("FailureDetails")?.ErrorMessage))
                    .ToList();

                var containsExpectedHistory = expectedHistory.Intersect(activities).Count() == expectedHistory.Count();
                return containsExpectedHistory;
            },
            TimeSpan.FromSeconds(30),
            delay: TimeSpan.FromSeconds(5));

        isExpected.Should().BeTrue("because we expect the actual history to contain the expected history");
    }

    private static ServiceBusMessage CreateCalculationCompletedEventMessage(
        string calculationOrchestrationId,
        CalculationCompletedV1.Types.CalculationType? calculationType = null,
        string? calculationId = null)
    {
        var calculationCompletedEvent = new CalculationCompletedV1
        {
            InstanceId = calculationOrchestrationId,
            CalculationId = calculationId ?? Guid.NewGuid().ToString(),
            CalculationType = calculationType ?? CalculationCompletedV1.Types.CalculationType.BalanceFixing,
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

    private async Task ClearAndAddInvalidDatabricksData(
        params TestDataDescription[] testDataDescriptions)
    {
        await ResetDatabricks();
        var ediDatabricksOptions = Options.Create(new EdiDatabricksOptions { DatabaseName = Fixture.DatabricksSchemaManager.SchemaName });

        var tasks = new List<Task>();
        foreach (var testDataDescription in testDataDescriptions)
        {
            IDeltaTableSchemaDescription schemaDescription;
            if (testDataDescription is EnergyResultPerGridAreaDescription)
            {
                schemaDescription = new EnergyResultPerGridAreaQuery(null!, ediDatabricksOptions.Value, null!, null!, testDataDescription.CalculationId);
            }
            else if (testDataDescription is EnergyResultPerBrpGridAreaDescription)
            {
                schemaDescription = new EnergyResultPerBalanceResponsiblePerGridAreaQuery(null!, ediDatabricksOptions.Value, null!, testDataDescription.CalculationId);
            }
            else if (testDataDescription is EnergyResultPerEnergySupplierBrpGridAreaDescription)
            {
                schemaDescription = new EnergyResultPerEnergySupplierPerBalanceResponsiblePerGridAreaQuery(null!, ediDatabricksOptions.Value, null!, testDataDescription.CalculationId);
            }
            else if (testDataDescription is WholesaleResultForAmountPerChargeDescription)
            {
                schemaDescription = new WholesaleAmountPerChargeQuery(null!, ediDatabricksOptions.Value, null!, null!, testDataDescription.CalculationId);
            }
            else if (testDataDescription is WholesaleResultForMonthlyAmountPerChargeDescription)
            {
                schemaDescription = new WholesaleMonthlyAmountPerChargeQuery(null!, ediDatabricksOptions.Value, null!, null!, testDataDescription.CalculationId);
            }
            else if (testDataDescription is WholesaleResultForTotalAmountDescription)
            {
                schemaDescription = new WholesaleTotalAmountQuery(null!, ediDatabricksOptions.Value, null!, testDataDescription.CalculationId);
            }
            else
            {
                throw new NotSupportedException($"Test data description of type '{testDataDescription.GetType()}' is not supported.");
            }

            tasks.Add(SeedDatabricksWithDataAsync(testDataDescription.TestFilePathWithAInvalidRow, schemaDescription));
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Adds hardcoded data to databricks.
    /// Notice we reuse test data from the `OutgoingMessagesClientTests`
    /// </summary>
    /// <returns>The calculation id of the hardcoded data which was added to databricks</returns>
    private async Task<Guid> ClearAndAddDatabricksData(
        EnergyResultPerGridAreaDescription perGridAreaDataDescription,
        EnergyResultPerBrpGridAreaDescription perBrpGridAreaDataDescription,
        EnergyResultPerEnergySupplierBrpGridAreaDescription perBrpAndEsGridAreaDataDescription,
        WholesaleResultForAmountPerChargeDescription forAmountPerChargeDescription,
        WholesaleResultForMonthlyAmountPerChargeDescription forMonthlyAmountPerChargeDescription,
        WholesaleResultForTotalAmountDescription forTotalAmountDescription)
    {
        // Ensure that databricks does not contain data, unless the test explicit adds it
        await ResetDatabricks();
        var ediDatabricksOptions = Options.Create(new EdiDatabricksOptions { DatabaseName = Fixture.DatabricksSchemaManager.SchemaName });

        // TODO: Seperate schema information from query
        var perGridAreaQuery = new EnergyResultPerGridAreaQuery(null!, ediDatabricksOptions.Value, null!, null!, perGridAreaDataDescription.CalculationId);
        var perGridAreTask = SeedDatabricksWithDataAsync(perGridAreaDataDescription.TestFilePath, perGridAreaQuery);

        var perBrpGridAreaQuery = new EnergyResultPerBalanceResponsiblePerGridAreaQuery(null!, ediDatabricksOptions.Value, null!, perGridAreaDataDescription.CalculationId);
        var perBrpGriaAreaTask = SeedDatabricksWithDataAsync(perBrpGridAreaDataDescription.TestFilePath, perBrpGridAreaQuery);

        var perBrpAndESGridAreaQuery = new EnergyResultPerEnergySupplierPerBalanceResponsiblePerGridAreaQuery(null!, ediDatabricksOptions.Value, null!, perGridAreaDataDescription.CalculationId);
        var perBrpAndESGridAreTask = SeedDatabricksWithDataAsync(perBrpAndEsGridAreaDataDescription.TestFilePath, perBrpAndESGridAreaQuery);

        var forAmountPerChargeQuery = new WholesaleAmountPerChargeQuery(null!, ediDatabricksOptions.Value, null!, null!, forAmountPerChargeDescription.CalculationId);
        var forAmountPerChargeTask = SeedDatabricksWithDataAsync(forAmountPerChargeDescription.TestFilePath, forAmountPerChargeQuery);

        var forMonthlyAmountPerChargeQuery = new WholesaleMonthlyAmountPerChargeQuery(null!, ediDatabricksOptions.Value, null!, null!, forMonthlyAmountPerChargeDescription.CalculationId);
        var forMonthlyAmountPerChargeTask = SeedDatabricksWithDataAsync(forMonthlyAmountPerChargeDescription.TestFilePath, forMonthlyAmountPerChargeQuery);

        var forTotalAmountQuery = new WholesaleTotalAmountQuery(null!, ediDatabricksOptions.Value, null!, forTotalAmountDescription.CalculationId);
        var forTotalAmountTask = SeedDatabricksWithDataAsync(forTotalAmountDescription.TestFilePath, forTotalAmountQuery);

        await Task.WhenAll(perGridAreTask, perBrpGriaAreaTask, perBrpAndESGridAreTask, forAmountPerChargeTask, forMonthlyAmountPerChargeTask, forTotalAmountTask);

        return perGridAreaDataDescription.CalculationId;
    }

    private async Task ResetDatabricks()
    {
        if (Fixture.DatabricksSchemaManager.SchemaExists)
            await Fixture.DatabricksSchemaManager.DropSchemaAsync();

        await Fixture.DatabricksSchemaManager.CreateSchemaAsync();
    }

    private async Task SeedDatabricksWithDataAsync(string testFilePath, IDeltaTableSchemaDescription schemaInformation)
    {
        await Fixture.DatabricksSchemaManager.CreateTableAsync(schemaInformation.DataObjectName, schemaInformation.SchemaDefinition);
        await Fixture.DatabricksSchemaManager.InsertFromCsvFileAsync(schemaInformation.DataObjectName, schemaInformation.SchemaDefinition, testFilePath);
    }

    private void EnableEnqueueMessagesOrchestration()
    {
        Fixture.EnsureAppHostUsesFeatureFlagValue(
            enableCalculationCompletedEvent: true,
            enableCalculationCompletedEventForBalanceFixing: true,
            enableCalculationCompletedEventForWholesaleFixing: true);
    }
}
