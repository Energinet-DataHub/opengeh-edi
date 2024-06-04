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

using Dapper;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.Core.Messaging.Communication.Subscriber;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Contracts;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages;

public class OutgoingMessagesClientTests : TestBase, IAsyncLifetime
{
    /// <summary>
    /// Located in 'Application\OutgoingMessages\TestData'
    /// </summary>
    private const string TestFilename = "balance_fixing_01-11-2022_01-12-2022_ga_543.csv";

    // Values matching test file values
    private readonly Guid _calculationId = Guid.Parse("e7a26e65-be5e-4db0-ba0e-a6bb4ae2ef3d");
    private readonly long _calculationVersion = 63;

    private readonly GridAreaOwnershipAssignedEventBuilder _gridAreaOwnershipAssignedEventBuilder = new();

    public OutgoingMessagesClientTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
    }

    public async Task InitializeAsync()
    {
        await Fixture.DatabricksSchemaManager.CreateSchemaAsync();

        var ediDatabricksOptions = GetService<IOptions<EdiDatabricksOptions>>();
        var viewQuery = new EnergyResultPerGridAreaQuery(ediDatabricksOptions, _calculationId);
        await Fixture.DatabricksSchemaManager.CreateTableAsync(viewQuery);

        var testFilePath = Path.Combine("Application", "OutgoingMessages", "TestData", TestFilename);
        await Fixture.DatabricksSchemaManager.InsertFromCsvFileAsync(viewQuery, testFilePath);
    }

    public async Task DisposeAsync()
    {
        await Fixture.DatabricksSchemaManager.DropSchemaAsync();
    }

    [Fact]
    public async Task GivenCalculationWithIdIsCompleted_WhenEnqueueByCalculationId_ThenOutgoingMessagesAreEnqueued()
    {
        var sut = GetService<IOutgoingMessagesClient>();
        var input = new EnqueueMessagesInputDto(
            _calculationId,
            _calculationVersion,
            EventId: Guid.NewGuid());

        var gridAreaOwnershipAssignedEvent01 = _gridAreaOwnershipAssignedEventBuilder
            .WithGridAreaCode("543")
            .Build();
        await HavingReceivedAndHandledIntegrationEventAsync(GridAreaOwnershipAssigned.EventName, gridAreaOwnershipAssignedEvent01);

        // Act
        await sut.EnqueueEnergyResultsForGridAreaOwnersAsync(input);

        // Assert
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = "SELECT * FROM [dbo].[OutgoingMessages]";
        var result = await connection.QueryAsync(sql);

        var actualCount = result.Count();
        actualCount.Should().Be(5);
    }

    private async Task HavingReceivedAndHandledIntegrationEventAsync(string eventType, GridAreaOwnershipAssigned gridAreaOwnershipAssigned)
    {
        var integrationEventHandler = GetService<IIntegrationEventHandler>();

        var integrationEvent = new IntegrationEvent(Guid.NewGuid(), eventType, 1, gridAreaOwnershipAssigned);

        await integrationEventHandler.HandleAsync(integrationEvent).ConfigureAwait(false);
    }
}
