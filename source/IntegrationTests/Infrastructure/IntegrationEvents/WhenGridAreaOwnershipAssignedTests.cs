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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.Core.Messaging.Communication.Subscriber;
using Energinet.DataHub.EDI.Application.Configuration.DataAccess;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Contracts;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.IntegrationEvents;

public class WhenGridAreaOwnershipAssignedTests : TestBase
{
    private readonly IDatabaseConnectionFactory _connectionFactory;

    public WhenGridAreaOwnershipAssignedTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _connectionFactory = GetService<IDatabaseConnectionFactory>();
    }

    [Fact]
    public async Task New_grid_area_event_is_received_stores_grid_area()
    {
        var gridAreaOwnershipAssignedEvent = GridAreaOwnershipAssignedEventBuilder.Build();

        await HavingReceivedAndHandledIntegrationEventAsync(GridAreaOwnershipAssigned.EventName, gridAreaOwnershipAssignedEvent);

        var gridAreas = await GetGridAreas(gridAreaOwnershipAssignedEvent.GridAreaCode, gridAreaOwnershipAssignedEvent.ActorNumber);
        Assert.Single(gridAreas);
    }

    [Fact]
    public async Task New_grid_area_event_is_received_with_existing_grid_area_code_does_not_store_second_grid_area()
    {
        var gridAreaOwnershipAssignedEvent = GridAreaOwnershipAssignedEventBuilder.Build();
        await HavingReceivedAndHandledIntegrationEventAsync(GridAreaOwnershipAssigned.EventName, gridAreaOwnershipAssignedEvent);

        await HavingReceivedAndHandledIntegrationEventAsync(GridAreaOwnershipAssigned.EventName, gridAreaOwnershipAssignedEvent);

        var gridAreas = await GetGridAreas(gridAreaOwnershipAssignedEvent.GridAreaCode, gridAreaOwnershipAssignedEvent.ActorNumber);
        Assert.Single(gridAreas);
    }

    private async Task HavingReceivedAndHandledIntegrationEventAsync(string eventType, GridAreaOwnershipAssigned gridAreaOwnershipAssigned)
    {
        var integrationEventHandler = GetService<IIntegrationEventHandler>();

        var integrationEvent = new IntegrationEvent(Guid.NewGuid(), eventType, 1, gridAreaOwnershipAssigned);

        await integrationEventHandler.HandleAsync(integrationEvent).ConfigureAwait(false);
    }

    private async Task<IEnumerable<GridArea>> GetGridAreas(string gridAreaCode, string gridAreaOwnerActorNumber)
    {
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = $"SELECT GridAreaCode, GridAreaOwnerActorNumber FROM [dbo].[GridArea] WHERE GridAreaOwnerActorNumber = '{gridAreaOwnerActorNumber}' AND GridAreaCode = '{gridAreaCode}'";
        return await connection.QueryAsync<GridArea>(sql);
    }

#pragma warning disable
    public record GridArea(string GridAreaCode, string GridAreaOwnerActorNumber);
#pragma warning restore
}
