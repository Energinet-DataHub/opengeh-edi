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

using System.Collections.Immutable;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.Core.DurableFunctionApp.TestCommon.DurableTask;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Databricks;
using Energinet.DataHub.Core.FunctionApp.TestCommon.ServiceBus.ListenerMock;
using Energinet.DataHub.Core.TestCommon;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.Behaviours.IntegrationEvents.TestData;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.SqlStatements;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Queries;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_023_027.V1.Model;
using Energinet.DataHub.ProcessManager.Shared.Extensions;
using Energinet.DataHub.Wholesale.Events.Infrastructure.IntegrationEvents;
using FluentAssertions;
using FluentAssertions.Execution;
using Google.Protobuf;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.B2BApi.AppTests.Functions.EnqueueMessages.BRS_023_027;

[Collection(nameof(B2BApiAppCollectionFixture))]
public class EnqueueMessagesOrchestrationViaPMTests : IAsyncLifetime
{
    private const string NotifyOrchestrationInstanceSubject = "NotifyOrchestration";
    private const string NotifyOrchestrationInstanceEventName = "ActorMessagesEnqueued"; // Hardcoded in the orchestration activity, fix it

    public EnqueueMessagesOrchestrationViaPMTests(
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
        Fixture.EnsureAppHostUsesFeatureFlagValue(enqueueBrs023027MessagesViaProcessManager: true);

        await AddGridAreaOwner(ActorNumber.Create("5790001662233"), "543");
        await AddGridAreaOwner(ActorNumber.Create("5790001662233"), "804");
    }

    public async Task DisposeAsync()
    {
        if (Fixture.EdiDatabricksSchemaManager.SchemaExists)
            await Fixture.EdiDatabricksSchemaManager.DropSchemaAsync();

        Fixture.SetTestOutputHelper(null!);
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

        var processManagerOrchestrationId = Guid.NewGuid();
        var calculationCompletedEvent = new CalculatedDataForCalculationTypeV1(
            CalculationId: calculationId,
            CalculationType: CalculationType.BalanceFixing);

        var serviceBusMessage = CreateEnqueueFromProcessManager(
            calculationCompletedEvent,
            processManagerOrchestrationId);

        // Act
        var beforeOrchestrationCreated = DateTime.UtcNow;
        await Fixture.EdiTopicResource.SenderClient.SendMessageAsync(serviceBusMessage);

        // Assert
        // => Verify expected behaviour by searching the orchestration history
        var actualOrchestrationStatus = await Fixture.DurableClient.WaitForOrchestationStartedAsync(createdTimeFrom: beforeOrchestrationCreated);

        // => Wait for completion
        var completeOrchestrationStatus = await Fixture.DurableClient.WaitForOrchestrationCompletedAsync(
            actualOrchestrationStatus.InstanceId,
            TimeSpan.FromMinutes(5));

        // => Expect history
        using var assertionScope = new AssertionScope();

        var activities = completeOrchestrationStatus.History
            .OrderBy(item => item["Timestamp"])
            .Select(item =>
                (item.Value<string>("FunctionName"), ValueIsArray(item) ? string.Join(',', item.Value<JArray>("Result")!.Select(x => x.ToString())) : item.Value<JToken>("Result")?.ToString()));

        activities.Should().NotBeNull().And.Contain(
        [
            ("EnqueueMessagesOrchestration", null),
            ("EnqueueEnergyResultsForGridAreaOwnersActivity", perGridAreaDataDescription.ExpectedCalculationResultsCount.ToString()),
            ("EnqueueEnergyResultsForBalanceResponsiblesActivity", perBrpGridAreaDataDescription.ExpectedCalculationResultsCount.ToString()),
            ("EnqueueEnergyResultsForBalanceResponsiblesAndEnergySuppliersActivity", perBrpAndEsGridAreaDataDescription.ExpectedCalculationResultsCount.ToString()),
            ("GetActorsForWholesaleResultsForAmountPerChargesActivity", string.Empty),
            ("GetActorsForWholesaleResultsForMonthlyAmountPerChargesActivity", string.Empty),
            ("GetActorsForWholesaleResultsForTotalAmountPerChargesActivity", string.Empty),
            ("SendActorMessagesEnqueuedActivity", string.Empty),
            (null, "Success"),
        ]);

        // => Verify that the durable function completed successfully
        completeOrchestrationStatus.RuntimeStatus.Should().Be(OrchestrationRuntimeStatus.Completed);

        // => Verify that the expected message was sent on the ServiceBus
        var verifyServiceBusMessages = await Fixture.ServiceBusListenerMock
            .When(msg =>
            {
                if (msg.Subject != NotifyOrchestrationInstanceSubject)
                {
                    return false;
                }

                var parsedNotification = NotifyOrchestrationInstanceV1.Parser.ParseJson(
                    msg.Body.ToString());

                var matchingOrchestrationId = parsedNotification.OrchestrationInstanceId == processManagerOrchestrationId.ToString();
                var matchingCalculationId = parsedNotification.EventName == NotifyEnqueueFinishedV1.EventName;
                var enqueueFinishedV1 = JsonConvert.DeserializeObject<NotifyEnqueueFinishedV1>(parsedNotification.Data.Data)!;

                return matchingOrchestrationId && matchingCalculationId && enqueueFinishedV1.Success;
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

        var processManagerOrchestrationId = Guid.NewGuid();
        var calculationCompletedEvent = new CalculatedDataForCalculationTypeV1(
            CalculationId: calculationId,
            CalculationType: CalculationType.BalanceFixing);

        var serviceBusMessage = CreateEnqueueFromProcessManager(
            calculationCompletedEvent,
            processManagerOrchestrationId);

        // Act
        var beforeOrchestrationCreated = DateTime.UtcNow;
        await Fixture.EdiTopicResource.SenderClient.SendMessageAsync(serviceBusMessage);

        // Assert
        // => Verify expected behaviour by searching the orchestration history
        var actualOrchestrationStatus = await Fixture.DurableClient.WaitForOrchestationStartedAsync(createdTimeFrom: beforeOrchestrationCreated);

        // => Wait for completion
        var completeOrchestrationStatus = await Fixture.DurableClient.WaitForOrchestrationCompletedAsync(
            actualOrchestrationStatus.InstanceId,
            TimeSpan.FromMinutes(5));

        // => Expect history
        using var assertionScope = new AssertionScope();

        Fixture.TestLogger.WriteLine($"Orchestration history:{Environment.NewLine}{completeOrchestrationStatus.History.ToString()}");

        var activities = completeOrchestrationStatus.History
            .OrderBy(item => item["Timestamp"])
            .Select(item =>
                (item.Value<string>("FunctionName"), ValueIsArray(item) ? string.Join(',', item.Value<JArray>("Result")!.Select(x => x.ToString())) : item.Value<JToken>("Result")?.ToString()));

        activities.Should()
            .NotBeNull()
            .And
            .Contain(
            [
                ("EnqueueMessagesOrchestration", null),
                ("EnqueueEnergyResultsForGridAreaOwnersActivity", "0"),
                ("EnqueueEnergyResultsForBalanceResponsiblesActivity", "0"),
                ("EnqueueEnergyResultsForBalanceResponsiblesAndEnergySuppliersActivity", "0"),
                ("GetActorsForWholesaleResultsForAmountPerChargesActivity", "5790001662233"),
                ("GetActorsForWholesaleResultsForMonthlyAmountPerChargesActivity", "5790001662233"),
                ("GetActorsForWholesaleResultsForTotalAmountPerChargesActivity", "5790001662233"),
                ("EnqueueWholesaleResultsForAmountPerChargesActivity", forAmountPerChargeDescription.ExpectedCalculationResultsCount.ToString()),
                ("EnqueueWholesaleResultsForMonthlyAmountPerChargesActivity", forMonthlyAmountPerChargeDescription.ExpectedCalculationResultsCount.ToString()),
                ("EnqueueWholesaleResultsForTotalAmountsActivity", forTotalAmountDescription.ExpectedCalculationResultsCount.ToString()),
                ("SendActorMessagesEnqueuedActivity", string.Empty),
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
                if (msg.Subject != NotifyOrchestrationInstanceSubject)
                {
                    return false;
                }

                var parsedNotification = NotifyOrchestrationInstanceV1.Parser.ParseJson(
                    msg.Body.ToString());

                var matchingOrchestrationId = parsedNotification.OrchestrationInstanceId == processManagerOrchestrationId.ToString();
                var matchingCalculationId = parsedNotification.EventName == NotifyEnqueueFinishedV1.EventName;
                var enqueueFinishedV1 = JsonConvert.DeserializeObject<NotifyEnqueueFinishedV1>(parsedNotification.Data.Data)!;

                return matchingOrchestrationId && matchingCalculationId && enqueueFinishedV1.Success;
            })
            .VerifyCountAsync(1);

        var wait = verifyServiceBusMessages.Wait(TimeSpan.FromSeconds(10));
        wait.Should().BeTrue("ActorMessagesEnqueuedV1 service bus message should be sent");
    }

    // /// <summary>
    // /// Verifies that:
    // /// - If databricks has no data for the CalculationId, then orchestration runs "forever" (because of retry policies).
    // /// </summary>
    // [Fact]
    // public async Task Given_DatabricksHasNoData_When_CalculationCompletedEventIsHandled_Then_OrchestrationIsStartedButActivitiesWillFailAndBeRetriedForever()
    // {
    //     // Arrange
    //     var calculationId = Guid.NewGuid().ToString();
    //     var calculationOrchestrationId = Guid.NewGuid().ToString();
    //     var calculationCompletedEventMessage = CreateCalculationCompletedEventMessage(
    //         calculationOrchestrationId,
    //         CalculationCompletedV1.Types.CalculationType.WholesaleFixing, // WholesaleFixing covers retries for both energy and wholesale results
    //         calculationId);
    //
    //     var expectedHistory = new List<(string? Name, string? EventType)>
    //     {
    //         ("EnqueueEnergyResultsForGridAreaOwnersActivity", "TaskFailed"),
    //         ("EnqueueEnergyResultsForBalanceResponsiblesActivity", "TaskFailed"),
    //         ("EnqueueEnergyResultsForBalanceResponsiblesAndEnergySuppliersActivity", "TaskFailed"),
    //         ("GetActorsForWholesaleResultsForAmountPerChargesActivity", "TaskFailed"),
    //         ("GetActorsForWholesaleResultsForMonthlyAmountPerChargesActivity", "TaskFailed"),
    //         ("GetActorsForWholesaleResultsForTotalAmountPerChargesActivity", "TaskFailed"),
    //     };
    //
    //     // Act
    //     var beforeOrchestrationCreated = DateTime.UtcNow;
    //     await Fixture.IntegrationEventsTopicResource.SenderClient.SendMessageAsync(calculationCompletedEventMessage);
    //
    //     // Assert
    //     // => Verify expected behaviour by searching the orchestration history
    //     var actualOrchestrationStatus = await Fixture.DurableClient.WaitForOrchestationStartedAsync(createdTimeFrom: beforeOrchestrationCreated);
    //
    //     // => Wait for running and expected history
    //     JArray? actualHistory = null;
    //     var isExpected = await Awaiter.TryWaitUntilConditionAsync(
    //         async () =>
    //         {
    //             var orchestrationStatus = await Fixture.DurableClient.GetStatusAsync(actualOrchestrationStatus.InstanceId, showHistory: true);
    //             actualHistory = orchestrationStatus.History;
    //
    //             if (orchestrationStatus.RuntimeStatus != OrchestrationRuntimeStatus.Running)
    //                 return false;
    //
    //             var history = orchestrationStatus.History
    //                 .OrderBy(item => item["Timestamp"])
    //                 .Select(item => new
    //                 {
    //                     Name = item.Value<string>("FunctionName"),
    //                     EventType = item.Value<string>("EventType"),
    //                 })
    //                 .ToList();
    //
    //             var containsExpectedHistoryAtleastTwice = expectedHistory
    //                 .All(expected => history
    //                     .Count(actual => actual.Name == expected.Name && actual.EventType == expected.EventType) > 1);
    //
    //             return containsExpectedHistoryAtleastTwice;
    //         },
    //         TimeSpan.FromSeconds(60),
    //         delay: TimeSpan.FromSeconds(5));
    //
    //     await Fixture.DurableClient.TerminateAsync(actualOrchestrationStatus.InstanceId,  reason: "Test is completed");
    //
    //     isExpected.Should().BeTrue($"because the history should contain the expected 6 failed activities atleast twice. Actual history: {actualHistory?.ToString() ?? "<null>"}");
    // }
    //
    // [Fact]
    // public async Task Given_WholesaleResultsContainsAnInvalidRow_When_CalculationCompletedEventForWholesaleFixing_Then_EnqueueAllValidMessages()
    // {
    //     // Arrange
    //     var forAmountPerChargeDescription = new WholesaleResultForAmountPerChargeDescription();
    //     var forMonthlyAmountPerChargeDescription = new WholesaleResultForMonthlyAmountPerChargeDescription();
    //     var forTotalAmountDescription = new WholesaleResultForTotalAmountDescription();
    //     await ClearAndAddInvalidDatabricksData(
    //         forAmountPerChargeDescription,
    //         forMonthlyAmountPerChargeDescription,
    //         forTotalAmountDescription);
    //     var wholesaleCalculationId = forAmountPerChargeDescription.CalculationId;
    //
    //     var wholesaleCalculationOrchestrationId = Guid.NewGuid().ToString();
    //     var wholesaleCalculationCompletedEventMessage = CreateCalculationCompletedEventMessage(
    //         wholesaleCalculationOrchestrationId,
    //         CalculationCompletedV1.Types.CalculationType.WholesaleFixing,
    //         wholesaleCalculationId.ToString());
    //
    //     // Act
    //     var beforeOrchestrationCreated = DateTime.UtcNow;
    //     await Fixture.IntegrationEventsTopicResource.SenderClient.SendMessageAsync(wholesaleCalculationCompletedEventMessage);
    //     var actualWholesaleOrchestrationStatus = await Fixture.DurableClient.WaitForOrchestationStartedAsync(createdTimeFrom: beforeOrchestrationCreated);
    //
    //     // Assert
    //     using var assertionScope = new AssertionScope();
    //     var wholesaleEventId = actualWholesaleOrchestrationStatus.Input["EventId"];
    //
    //     var expectedHistory = new List<(string?, string?)>
    //     {
    //         ("EnqueueWholesaleResultsForAmountPerChargesActivity", $"Enqueue messages activity failed. Actor='{forAmountPerChargeDescription.ExampleWholesaleResultMessageData.EnergySupplier.Value}' CalculationId='{wholesaleCalculationId}' EventId='{wholesaleEventId}' NumberOfFailedResults='1' NumberOfHandledResults='{forAmountPerChargeDescription.ExpectedCalculationResultsCount - 1}'"),
    //         ("EnqueueWholesaleResultsForMonthlyAmountPerChargesActivity", $"Enqueue messages activity failed. Actor='{forMonthlyAmountPerChargeDescription.ExampleWholesaleResultMessageDataForEnergySupplier.EnergySupplier.Value}' CalculationId='{wholesaleCalculationId}' EventId='{wholesaleEventId}' NumberOfFailedResults='1' NumberOfHandledResults='{forMonthlyAmountPerChargeDescription.ExpectedCalculationResultsCount - 1}'"),
    //         ("EnqueueWholesaleResultsForTotalAmountsActivity", $"Enqueue messages activity failed. Actor='{forTotalAmountDescription.ExampleWholesaleResultMessageDataForEnergySupplier.EnergySupplier.Value}' CalculationId='{wholesaleCalculationId}' EventId='{wholesaleEventId}' NumberOfFailedResults='1' NumberOfHandledResults='{forTotalAmountDescription.ExpectedCalculationResultsCount - 1}'"),
    //     };
    //
    //     // => Wait for running and expected history
    //     JArray? actualHistory = null;
    //     var isExpected = await Awaiter.TryWaitUntilConditionAsync(
    //         async () =>
    //         {
    //             var orchestrationStatus = await Fixture.DurableClient.GetStatusAsync(actualWholesaleOrchestrationStatus.InstanceId, showHistory: true);
    //             actualHistory = orchestrationStatus.History;
    //
    //             if (orchestrationStatus.RuntimeStatus != OrchestrationRuntimeStatus.Running)
    //                 return false;
    //
    //             var activities = orchestrationStatus.History
    //                 .OrderBy(item => item["Timestamp"])
    //                 .Where(item => item.HasValues)
    //                 .Select(item =>
    //                     (item.Value<string>("FunctionName"), (string?)item.Value<dynamic>("FailureDetails")?.ErrorMessage))
    //                 .ToList();
    //
    //             var containsExpectedHistory = expectedHistory.Intersect(activities).Count() == expectedHistory.Count();
    //             return containsExpectedHistory;
    //         },
    //         TimeSpan.FromSeconds(60),
    //         delay: TimeSpan.FromSeconds(5));
    //
    //     await Fixture.DurableClient.TerminateAsync(actualWholesaleOrchestrationStatus.InstanceId, reason: "Test is completed");
    //
    //     isExpected.Should().BeTrue($"because the history should contain the expected 3 failed wholesale result activities. Actual history: {actualHistory?.ToString() ?? "<null>"}");
    // }
    [Fact]
    public async Task Given_EnergyResultsContainsAnInvalidRow_When_CalculationCompletedEventForBalanceFixing_Then_EnqueueAllValidMessages()
    {
        // Arrange
        var perGridAreaDataDescription = new EnergyResultPerGridAreaDescription();
        var perBrpGridAreaDataDescription = new EnergyResultPerBrpGridAreaDescription();
        var perBrpAndEsGridAreaDataDescription = new EnergyResultPerEnergySupplierBrpGridAreaDescription();
        await ClearAndAddInvalidDatabricksData(
            perGridAreaDataDescription,
            perBrpGridAreaDataDescription,
            perBrpAndEsGridAreaDataDescription);
        var energyCalculationId = perBrpAndEsGridAreaDataDescription.CalculationId;

        var calculationCompletedEvent = new CalculatedDataForCalculationTypeV1(
            CalculationId: energyCalculationId,
            CalculationType: CalculationType.BalanceFixing);

        var serviceBusMessage = CreateEnqueueFromProcessManager(calculationCompletedEvent);

        // Act
        var beforeOrchestrationCreated = DateTime.UtcNow;
        await Fixture.EdiTopicResource.SenderClient.SendMessageAsync(serviceBusMessage);
        var actualEnergyOrchestrationStatus = await Fixture.DurableClient.WaitForOrchestationStartedAsync(
            createdTimeFrom: beforeOrchestrationCreated);

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
        JArray? actualHistory = null;
        var isExpected = await Awaiter.TryWaitUntilConditionAsync(
            async () =>
            {
                var orchestrationStatus = await Fixture.DurableClient.GetStatusAsync(
                    actualEnergyOrchestrationStatus.InstanceId,
                    showHistory: true);
                actualHistory = orchestrationStatus.History;

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

        await Fixture.DurableClient.TerminateAsync(actualEnergyOrchestrationStatus.InstanceId, reason: "Test is completed");

        isExpected.Should().BeTrue($"because the history should contain the expected 3 failed energy result activities. Actual history: {actualHistory?.ToString() ?? "<null>"}");
    }

    private static bool ValueIsArray(JToken item)
    {
        return item.Value<JToken>("Result")?.Type == JTokenType.Array;
    }

    private static ServiceBusMessage CreateEnqueueFromProcessManager(
        CalculatedDataForCalculationTypeV1 calculationCompletedEvent,
        Guid? orchestrationInstanceId = null)
    {
        var actorId = Guid.NewGuid().ToString();
        var enqueueActorMessages = new EnqueueActorMessagesV1
        {
            OrchestrationName = "Brs_023_027",
            OrchestrationVersion = 1,
            OrchestrationStartedByActorId = actorId,
            OrchestrationInstanceId = orchestrationInstanceId?.ToString() ?? Guid.NewGuid().ToString(),
        };
        enqueueActorMessages.SetData(calculationCompletedEvent);
        return enqueueActorMessages.ToServiceBusMessage(
            subject: $"Enqueue_{enqueueActorMessages.OrchestrationName.ToLower()}",
            idempotencyKey: "a-message-id");
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
        var ediDatabricksOptions = Options.Create(new EdiDatabricksOptions { DatabaseName = Fixture.EdiDatabricksSchemaManager.SchemaName });

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
                schemaDescription = new WholesaleAmountPerChargeQuery(null!, ediDatabricksOptions.Value, ImmutableDictionary<string, ActorNumber>.Empty, null!, testDataDescription.CalculationId, null);
            }
            else if (testDataDescription is WholesaleResultForMonthlyAmountPerChargeDescription)
            {
                schemaDescription = new WholesaleMonthlyAmountPerChargeQuery(null!, ediDatabricksOptions.Value, ImmutableDictionary<string, ActorNumber>.Empty, null!, testDataDescription.CalculationId, null);
            }
            else if (testDataDescription is WholesaleResultForTotalAmountDescription)
            {
                schemaDescription = new WholesaleTotalAmountQuery(null!, ediDatabricksOptions.Value,  ImmutableDictionary<string, ActorNumber>.Empty, null!, testDataDescription.CalculationId, null);
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
        var ediDatabricksOptions = Options.Create(new EdiDatabricksOptions { DatabaseName = Fixture.EdiDatabricksSchemaManager.SchemaName });

        // TODO: Separate schema information from query
        var perGridAreaQuery = new EnergyResultPerGridAreaQuery(null!, ediDatabricksOptions.Value, null!, null!, perGridAreaDataDescription.CalculationId);
        var perGridAreTask = SeedDatabricksWithDataAsync(perGridAreaDataDescription.TestFilePath, perGridAreaQuery);

        var perBrpGridAreaQuery = new EnergyResultPerBalanceResponsiblePerGridAreaQuery(null!, ediDatabricksOptions.Value, null!, perGridAreaDataDescription.CalculationId);
        var perBrpGridAreaTask = SeedDatabricksWithDataAsync(perBrpGridAreaDataDescription.TestFilePath, perBrpGridAreaQuery);

        var perBrpAndESGridAreaQuery = new EnergyResultPerEnergySupplierPerBalanceResponsiblePerGridAreaQuery(null!, ediDatabricksOptions.Value, null!, perGridAreaDataDescription.CalculationId);
        var perBrpAndESGridAreTask = SeedDatabricksWithDataAsync(perBrpAndEsGridAreaDataDescription.TestFilePath, perBrpAndESGridAreaQuery);

        var forAmountPerChargeQuery = new WholesaleAmountPerChargeQuery(null!, ediDatabricksOptions.Value, forAmountPerChargeDescription.GridAreaOwners, null!, forAmountPerChargeDescription.CalculationId, null);
        var forAmountPerChargeTask = SeedDatabricksWithDataAsync(forAmountPerChargeDescription.TestFilePath, forAmountPerChargeQuery);

        var forMonthlyAmountPerChargeQuery = new WholesaleMonthlyAmountPerChargeQuery(null!, ediDatabricksOptions.Value, forMonthlyAmountPerChargeDescription.GridAreaOwners, null!, forMonthlyAmountPerChargeDescription.CalculationId, null);
        var forMonthlyAmountPerChargeTask = SeedDatabricksWithDataAsync(forMonthlyAmountPerChargeDescription.TestFilePath, forMonthlyAmountPerChargeQuery);

        var forTotalAmountQuery = new WholesaleTotalAmountQuery(null!, ediDatabricksOptions.Value, forTotalAmountDescription.GridAreaOwners, null!, forTotalAmountDescription.CalculationId, null);
        var forTotalAmountTask = SeedDatabricksWithDataAsync(forTotalAmountDescription.TestFilePath, forTotalAmountQuery);

        await Task.WhenAll(perGridAreTask, perBrpGridAreaTask, perBrpAndESGridAreTask, forAmountPerChargeTask, forMonthlyAmountPerChargeTask, forTotalAmountTask);

        return perGridAreaDataDescription.CalculationId;
    }

    private async Task ResetDatabricks()
    {
        if (Fixture.EdiDatabricksSchemaManager.SchemaExists)
            await Fixture.EdiDatabricksSchemaManager.DropSchemaAsync();

        await Fixture.EdiDatabricksSchemaManager.CreateSchemaAsync();
    }

    private async Task SeedDatabricksWithDataAsync(string testFilePath, IDeltaTableSchemaDescription schemaInformation)
    {
        await Fixture.EdiDatabricksSchemaManager.CreateTableAsync(schemaInformation.DataObjectName, schemaInformation.SchemaDefinition);
        await Fixture.EdiDatabricksSchemaManager.InsertFromCsvFileAsync(schemaInformation.DataObjectName, schemaInformation.SchemaDefinition, testFilePath);
    }

    private async Task AddGridAreaOwner(ActorNumber actorNumber, string gridAreaCode)
    {
        await Fixture.DatabaseManager.AddGridAreaOwnerAsync(actorNumber, gridAreaCode);
    }
}
