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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages.TestData;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.SqlStatements;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Queries;
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
    private readonly GridAreaOwnershipAssignedEventBuilder _gridAreaOwnershipAssignedEventBuilder = new();

    public OutgoingMessagesClientTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
    }

    public async Task InitializeAsync()
    {
        await Fixture.DatabricksSchemaManager.CreateSchemaAsync();
    }

    public async Task DisposeAsync()
    {
        await Fixture.DatabricksSchemaManager.DropSchemaAsync();
    }

    [Fact]
    public async Task GivenCalculationWithIdIsCompleted_WhenEnqueueEnergyResultsForGridAreaOwners_ThenOutgoingMessagesAreEnqueued()
    {
        var testDataDescription = new EnergyResultPerGridAreaDescription();

        var ediDatabricksOptions = GetService<IOptions<EdiDatabricksOptions>>();
        var viewQuery = new EnergyResultPerGridAreaQuery(ediDatabricksOptions.Value, GetService<IMasterDataClient>(), EventId.From(Guid.NewGuid()), testDataDescription.CalculationId);

        await HavingReceivedAndHandledGridAreaOwnershipAssignedEventAsync(testDataDescription.GridAreaCode);
        await SeedDatabricksWithDataAsync(testDataDescription, viewQuery);

        var sut = GetService<IOutgoingMessagesClient>();
        var input = new EnqueueMessagesInputDto(
            testDataDescription.CalculationId,
            EventId: EventId.From(Guid.NewGuid()));

        // Act
        await sut.EnqueueEnergyResultsPerGridAreaAsync(input);

        // Assert
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = "SELECT * FROM [dbo].[OutgoingMessages]";
        var result = await connection.QueryAsync(sql);

        var actualCount = result.Count();
        actualCount.Should().Be(testDataDescription.ExpectedOutgoingMessagesCount);
    }

    [Fact]
    public async Task GivenCalculationWithIdIsCompleted_WhenEnqueueEnergyResultsForBalanceResponsibles_ThenOutgoingMessagesAreEnqueued()
    {
        var testDataDescription = new EnergyResultPerBrpGridAreaDescription();

        var ediDatabricksOptions = GetService<IOptions<EdiDatabricksOptions>>();
        var viewQuery = new EnergyResultPerBalanceResponsiblePerGridAreaQuery(ediDatabricksOptions.Value, EventId.From(Guid.NewGuid()), testDataDescription.CalculationId);

        await SeedDatabricksWithDataAsync(testDataDescription, viewQuery);

        var sut = GetService<IOutgoingMessagesClient>();
        var input = new EnqueueMessagesInputDto(
            testDataDescription.CalculationId,
            EventId: EventId.From(Guid.NewGuid()));

        // Act
        await sut.EnqueueEnergyResultsPerBalanceResponsibleAsync(input);

        // Assert
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = "SELECT * FROM [dbo].[OutgoingMessages]";
        var result = await connection.QueryAsync(sql);

        var actualCount = result.Count();
        actualCount.Should().Be(testDataDescription.ExpectedOutgoingMessagesCount);
    }

    [Fact]
    public async Task GivenCalculationWithIdIsCompleted_WhenEnqueueEnergyResultsForBalanceResponsiblesAndEnergySuppliers_ThenOutgoingMessagesAreEnqueued()
    {
        var testDataDescription = new EnergyResultPerEnergySupplierBrpGridAreaDescription();

        var ediDatabricksOptions = GetService<IOptions<EdiDatabricksOptions>>();
        var viewQuery = new EnergyResultPerEnergySupplierPerBalanceResponsiblePerGridAreaQuery(ediDatabricksOptions.Value, EventId.From(Guid.NewGuid()), testDataDescription.CalculationId);

        await SeedDatabricksWithDataAsync(testDataDescription, viewQuery);

        var sut = GetService<IOutgoingMessagesClient>();
        var input = new EnqueueMessagesInputDto(
            testDataDescription.CalculationId,
            EventId: EventId.From(Guid.NewGuid()));

        // Act
        await sut.EnqueueEnergyResultsPerEnergySupplierPerBalanceResponsibleAsync(input);

        // Assert
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = "SELECT * FROM [dbo].[OutgoingMessages]";
        var result = await connection.QueryAsync(sql);

        var actualCount = result.Count();
        actualCount.Should().Be(testDataDescription.ExpectedOutgoingMessagesCount);
    }

    [Fact]
    public async Task GivenCalculationWithIdIsCompleted_WhenEnqueueWholesaleServicesForEnergySupplierAndGridOwner_ThenOutgoingMessagesAreEnqueued()
    {
        var testDataDescription = new WholesaleResultForAmountPerChargeDescription();

        var ediDatabricksOptions = GetService<IOptions<EdiDatabricksOptions>>();
        var viewQuery = new WholesaleAmountPerChargeQuery(ediDatabricksOptions.Value, GetService<IMasterDataClient>(), EventId.From(Guid.NewGuid()), testDataDescription.CalculationId);

        await HavingReceivedAndHandledGridAreaOwnershipAssignedEventAsync(testDataDescription.GridAreaCode);
        await SeedDatabricksWithDataAsync(testDataDescription, viewQuery);

        var sut = GetService<IOutgoingMessagesClient>();
        var input = new EnqueueMessagesInputDto(
            testDataDescription.CalculationId,
            EventId: EventId.From(Guid.NewGuid()));

        // Act
        await sut.EnqueueWholesaleResultsForAmountPerChargeAsync(input);

        // Assert
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = "SELECT * FROM [dbo].[OutgoingMessages]";
        var result = await connection.QueryAsync(sql);

        var actualCount = result.Count();
        actualCount.Should().Be(testDataDescription.ExpectedOutgoingMessagesCount);
    }

    private async Task SeedDatabricksWithDataAsync(TestDataDescription testDataDescription, IDeltaTableSchemaDescription schemaInfomation)
    {
        await Fixture.DatabricksSchemaManager.CreateTableAsync(schemaInfomation);
        await Fixture.DatabricksSchemaManager.InsertFromCsvFileAsync(schemaInfomation, testDataDescription.TestFilePath);
    }

    private async Task HavingReceivedAndHandledGridAreaOwnershipAssignedEventAsync(string gridAreaCode)
    {
        // The grid area in the mocked data needs an owner. Since the messages need a receiver.
        var gridAreaOwnershipAssignedEvent01 = _gridAreaOwnershipAssignedEventBuilder
            .WithGridAreaCode(gridAreaCode)
            .Build();

        var integrationEventHandler = GetService<IIntegrationEventHandler>();

        var integrationEvent = new IntegrationEvent(
            Guid.NewGuid(),
            GridAreaOwnershipAssigned.EventName,
            EventMinorVersion: 1,
            gridAreaOwnershipAssignedEvent01);

        await integrationEventHandler.HandleAsync(integrationEvent).ConfigureAwait(false);
    }
}
