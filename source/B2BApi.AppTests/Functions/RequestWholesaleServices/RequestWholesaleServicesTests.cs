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

        // Clear mappings etc. before each test
        await AddGridAreaOwner(ActorNumber.Create("5790001662233"), "804");
    }

    public async Task DisposeAsync()
    {
        if (Fixture.DatabricksSchemaManager.SchemaExists)
            await Fixture.DatabricksSchemaManager.DropSchemaAsync();

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
    public async Task When_RequestWholesaleServicesDocumentReceived_Then_OrchestrationCompletesWithExpectedMessagesEnqueued()
    {
        // Arrange
        EnableRequestWholesaleServicesOrchestrationFeature();
        // The following must match with the JSON/XML document content
        var actor = new Actor(
            ActorNumber.Create("5790000392551"),
            ActorRole.EnergySupplier);

        var amountPerChargeDescription = new WholesaleResultForAmountPerChargeDescription();
        await ClearAndAddDatabricksData(amountPerChargeDescription);

        // Test steps:
        // => HTTP POST: RequestWholesaleServices
        var beforeOrchestrationCreated = SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromSeconds(30));
        var httpRequest = await Fixture.CreateRequestWholesaleServicesHttpRequestAsync(actor);
        var httpResponse = await Fixture.AppHostManager.HttpClient.SendAsync(httpRequest);
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

        // => Assert activities
        // => Assert enqueued messages (database or peek?)
        using var assertionScope = new AssertionScope();
        completedOrchestrationStatus.RuntimeStatus.Should().Be(OrchestrationRuntimeStatus.Completed);
        completedOrchestrationStatus.Output.ToString()
            .Should().Contain("AcceptedMessagesCount=1")
            .And.Contain("RejectedMessagesCount=0");
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
        var ediDatabricksOptions = Options.Create(new EdiDatabricksOptions { DatabaseName = Fixture.DatabricksSchemaManager.SchemaName });

        var amountPerChargeQuery = new WholesaleAmountPerChargeQuery(null!, ediDatabricksOptions.Value, null!, null!, amountPerChargeDescription.CalculationId, null);
        await SeedDatabricksWithDataAsync(amountPerChargeDescription.TestFilePath, amountPerChargeQuery);

        return amountPerChargeDescription.CalculationId;
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

    private async Task AddGridAreaOwner(ActorNumber actorNumber, string gridAreaCode)
    {
        await Fixture.DatabaseManager.AddGridAreaOwnerAsync(actorNumber, gridAreaCode);
    }

    private void EnableRequestWholesaleServicesOrchestrationFeature()
    {
        Fixture.EnsureAppHostUsesFeatureFlagValue(useRequestWholesaleServicesOrchestration: true);
    }
}
