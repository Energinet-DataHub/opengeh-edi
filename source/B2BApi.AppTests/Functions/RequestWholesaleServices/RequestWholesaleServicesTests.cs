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

using System.Diagnostics;
using System.Net;
using Energinet.DataHub.BuildingBlocks.Tests.Logging;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Databricks;
using Energinet.DataHub.EDI.B2BApi.AppTests.DurableTask;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures;
using Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures.Extensions;
using Energinet.DataHub.EDI.B2BApi.Functions.RequestWholesaleServices;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IntegrationTests.Behaviours.IntegrationEvents.TestData;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.SqlStatements;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Queries;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Options;
using NodaTime;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.B2BApi.AppTests.Functions.RequestWholesaleServices;

[Collection(nameof(B2BApiAppCollectionFixture))]
public class RequestWholesaleServicesTests : IAsyncLifetime
{
    public RequestWholesaleServicesTests(
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
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        Fixture.SetTestOutputHelper(null!);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Verifies that:
    ///  - The RequestWholesaleServicesOrchestration can complete successfully.
    /// - The correct number of messages are enqueued.
    /// - The enqueued messages can all be peeked and dequeued.
    /// - The peeked messages all have the correct document type.
    /// </summary>
    [Fact]
    public async Task Given_RequestWholesaleServices_When_RequestWholesaleServicesOrchestrationIsCompleted_Then_EnqueuedMessagesCanBePeeked()
    {
        // Arrange
        EnableRequestWholesaleServicesOrchestrationFeature();
        // The following must match with the JSON/XML document content
        var energySupplier = new Actor(
            ActorNumber.Create("5790000701278"),
            ActorRole.EnergySupplier);

        var amountPerChargeDescription = new WholesaleResultForAmountPerChargeDescription();
        await ClearAndAddDatabricksData(amountPerChargeDescription);

        // Test steps:
        // => HTTP POST: RequestWholesaleServices
        var beforeOrchestrationCreated = SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromSeconds(30));
        using var httpRequest = await Fixture.CreateRequestWholesaleServicesHttpRequestAsync(energySupplier);
        using var httpResponse = await Fixture.AppHostManager.HttpClient.SendAsync(httpRequest);
        await httpResponse.EnsureSuccessStatusCodeWithLogAsync(Fixture.TestLogger);

        // => Wait for orchestration to start
        var startedOrchestrationStatus = await Fixture.DurableClient.WaitForOrchestrationStatusAsync(
            createdTimeFrom: beforeOrchestrationCreated.ToDateTimeUtc(),
            name: nameof(RequestWholesaleServicesOrchestration));
        startedOrchestrationStatus.Should().NotBeNull();

        // => Wait for orchestration to complete
        var completedOrchestrationStatus = await Fixture.DurableClient.WaitForInstanceCompletedAsync(
            startedOrchestrationStatus.InstanceId,
            TimeSpan.FromMinutes(5));
        completedOrchestrationStatus.Should().NotBeNull();

        // Assert activities
        // => Assert enqueued messages in orchestrator
        using (new AssertionScope())
        {
            completedOrchestrationStatus.RuntimeStatus.Should().Be(OrchestrationRuntimeStatus.Completed);
            completedOrchestrationStatus.Output.ToString()
                .Should().Contain("AcceptedMessagesCount=33")
                .And.Contain("RejectedMessagesCount=0");
        }

         // => HTTP GET: Peek messages
        var peekedDocuments = await PeekAllMessages(energySupplier);

        using var assertionScope = new AssertionScope();
        peekedDocuments.Should()
            .HaveCount(33)
            .And.AllSatisfy(
                peekedDocument => peekedDocument.Should().Contain("NotifyWholesaleServices_MarketDocument"));
    }

    private async Task<IReadOnlyCollection<string>> PeekAllMessages(Actor actor)
    {
        List<string> peekResponses = [];

        var timeout = TimeSpan.FromSeconds(60);
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            using var peekResponse = await PeekNextMessage(actor);

            if (peekResponse.StatusCode == HttpStatusCode.NoContent)
                break;

            var peekedDocument = await peekResponse.Content.ReadAsStringAsync();
            peekResponses.Add(peekedDocument);

            var messageId = peekResponse.Headers.GetValues("MessageId").Single();
            await DequeueMessage(actor, messageId);
        }

        return peekResponses;
    }

    private async Task<HttpResponseMessage> PeekNextMessage(Actor actor)
    {
        HttpResponseMessage? peekResponse = null;
        try
        {
            using var peekRequest = await Fixture.CreatePeekHttpRequestAsync(actor);
            peekResponse = await Fixture.AppHostManager.HttpClient.SendAsync(peekRequest);
            await peekResponse.EnsureSuccessStatusCodeWithLogAsync(Fixture.TestLogger);
            return peekResponse;
        }
        catch
        {
            peekResponse?.Dispose();
            throw;
        }
    }

    private async Task DequeueMessage(Actor actor, string messageId)
    {
        using var dequeueRequest = await Fixture.CreateDequeueHttpRequestAsync(
            actor,
            messageId);

        using var dequeueResponse = await Fixture.AppHostManager.HttpClient.SendAsync(dequeueRequest);
        await dequeueResponse.EnsureSuccessStatusCodeWithLogAsync(Fixture.TestLogger);
    }

    /// <summary>
    /// Adds hardcoded data to databricks.
    /// Notice we reuse test data from the `OutgoingMessagesClientTests`
    /// </summary>
    /// <returns>The calculation id of the hardcoded data which was added to databricks</returns>
    private async Task<Guid> ClearAndAddDatabricksData(WholesaleResultForAmountPerChargeDescription amountPerChargeDescription)
    {
        // Ensure that databricks does not contain data, unless the test explicit adds it
        await ResetDatabricks();
        var ediDatabricksOptions = Options.Create(new EdiDatabricksOptions { DatabaseName = Fixture.EdiDatabricksSchemaManager.SchemaName });

        var amountPerChargeQuery = new WholesaleAmountPerChargeQuery(null!, ediDatabricksOptions.Value, null!, null!, amountPerChargeDescription.CalculationId, null);
        await SeedDatabricksWithDataAsync(amountPerChargeDescription.TestFilePath, amountPerChargeQuery);

        return amountPerChargeDescription.CalculationId;
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

    private void EnableRequestWholesaleServicesOrchestrationFeature()
    {
        Fixture.EnsureAppHostUsesFeatureFlagValue(useRequestWholesaleServicesOrchestration: true);
    }
}
