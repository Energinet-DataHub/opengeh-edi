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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.Core.Messaging.Communication.Subscriber;
using Energinet.DataHub.EDI.Common.DataAccess;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Contracts;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.IntegrationEvents;

public class WhenGridAreaOwnershipAssignedTests : TestBase
{
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly GridAreaOwnershipAssignedEventBuilder _gridAreaOwnershipAssignedEventBuilder = new();

    public WhenGridAreaOwnershipAssignedTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _connectionFactory = GetService<IDatabaseConnectionFactory>();
    }

    [Fact]
    public async Task New_grid_area_event_is_received_stores_grid_area()
    {
        var gridAreaOwnershipAssignedEvent = _gridAreaOwnershipAssignedEventBuilder.Build();

        await HavingReceivedAndHandledIntegrationEventAsync(GridAreaOwnershipAssigned.EventName, gridAreaOwnershipAssignedEvent);

        var gridAreas = await GetGridAreas();
        Assert.Single(gridAreas);
    }

    [Fact]
    public async Task Multiple_grid_area_ownership_assigned_event_is_received_with_same_owner_is_stored()
    {
        var gridAreaOwnershipAssignedEvent01 = _gridAreaOwnershipAssignedEventBuilder
            .WithGridAreaCode("543")
            .Build();
        var gridAreaOwnershipAssignedEvent02 = _gridAreaOwnershipAssignedEventBuilder
            .WithGridAreaCode("804")
            .Build();

        await HavingReceivedAndHandledIntegrationEventAsync(GridAreaOwnershipAssigned.EventName, gridAreaOwnershipAssignedEvent01);
        await HavingReceivedAndHandledIntegrationEventAsync(GridAreaOwnershipAssigned.EventName, gridAreaOwnershipAssignedEvent02);

        var gridAreas = await GetGridAreas();
        Assert.All(gridAreas, area => Assert.Equal(area.GridAreaOwnerActorNumber, gridAreaOwnershipAssignedEvent01.ActorNumber));
    }

    [Fact]
    public async Task New_grid_area_event_for_existing_grid_area_code_with_newer_valid_from_update_ownership()
    {
        var newerValidFrom = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(1));
        var gridAreaOwnershipAssignedEvent01 = _gridAreaOwnershipAssignedEventBuilder
            .Build();
        var gridAreaOwnershipAssignedEvent02 = _gridAreaOwnershipAssignedEventBuilder
            .WithOwnerShipActorNumber("9876543210987")
            .WithValidFrom(newerValidFrom)
            .Build();
        await HavingReceivedAndHandledIntegrationEventAsync(GridAreaOwnershipAssigned.EventName, gridAreaOwnershipAssignedEvent01);

        await HavingReceivedAndHandledIntegrationEventAsync(GridAreaOwnershipAssigned.EventName, gridAreaOwnershipAssignedEvent02);

        var gridAreas = await GetGridAreas();
        var gridArea = gridAreas.Single();
        Assert.Equal(gridArea.GridAreaCode, gridAreaOwnershipAssignedEvent02.GridAreaCode);
        Assert.Equal(gridArea.GridAreaOwnerActorNumber, gridAreaOwnershipAssignedEvent02.ActorNumber);
    }

    [Fact]
    public async Task New_grid_area_event_for_existing_grid_area_code_with_old_valid_from_does_not_update_ownership()
    {
        var olderValidFrom = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var gridAreaOwnershipAssignedEvent01 = _gridAreaOwnershipAssignedEventBuilder
            .Build();
        var gridAreaOwnershipAssignedEvent02 = _gridAreaOwnershipAssignedEventBuilder
            .WithValidFrom(olderValidFrom)
            .WithOwnerShipActorNumber("9876543210987")
            .Build();
        await HavingReceivedAndHandledIntegrationEventAsync(GridAreaOwnershipAssigned.EventName, gridAreaOwnershipAssignedEvent01);

        await HavingReceivedAndHandledIntegrationEventAsync(GridAreaOwnershipAssigned.EventName, gridAreaOwnershipAssignedEvent02);

        var gridAreas = await GetGridAreas();
        var gridArea = gridAreas.Single();
        Assert.Equal(gridArea.GridAreaCode, gridAreaOwnershipAssignedEvent01.GridAreaCode);
        Assert.Equal(gridArea.GridAreaOwnerActorNumber, gridAreaOwnershipAssignedEvent01.ActorNumber);
    }

    [Fact]
    public async Task Multiple_grid_area_ownership_events_with_same_valid_from_highest_sequence_number_is_stored()
    {
        const string newGridAreaOwner = "9876543210987";
        var validFrom = Timestamp.FromDateTime(DateTime.UtcNow);
        var gridAreaOwnershipAssignedEvent01 = _gridAreaOwnershipAssignedEventBuilder
            .WithValidFrom(validFrom)
            .WithSequenceNumber(1)
            .Build();
        var gridAreaOwnershipAssignedEvent02 = _gridAreaOwnershipAssignedEventBuilder
            .WithOwnerShipActorNumber(newGridAreaOwner)
            .WithValidFrom(validFrom)
            .WithSequenceNumber(2)
            .Build();

        await HavingReceivedAndHandledIntegrationEventAsync(GridAreaOwnershipAssigned.EventName, gridAreaOwnershipAssignedEvent01);

        await HavingReceivedAndHandledIntegrationEventAsync(GridAreaOwnershipAssigned.EventName, gridAreaOwnershipAssignedEvent02);

        var gridAreas = await GetGridAreas();
        var gridArea = gridAreas.Single();
        Assert.Equal(newGridAreaOwner, gridArea.GridAreaOwnerActorNumber);
    }

    [Fact]
    public async Task Multiple_grid_area_ownership_events_with_same_valid_from_highest_sequence_number_is_stored_received_reverse_order()
    {
        const string gridAreaOwner1 = "9876543210987";
        const string gridAreaOwner2 = "1234567890123";
        var validFrom = Timestamp.FromDateTime(DateTime.UtcNow);
        var gridAreaOwnershipAssignedEvent01 = _gridAreaOwnershipAssignedEventBuilder
            .WithOwnerShipActorNumber(gridAreaOwner2)
            .WithValidFrom(validFrom)
            .WithSequenceNumber(2)
            .Build();
        var gridAreaOwnershipAssignedEvent02 = _gridAreaOwnershipAssignedEventBuilder
            .WithOwnerShipActorNumber(gridAreaOwner1)
            .WithValidFrom(validFrom)
            .WithSequenceNumber(1)
            .Build();

        await HavingReceivedAndHandledIntegrationEventAsync(GridAreaOwnershipAssigned.EventName, gridAreaOwnershipAssignedEvent01);

        await HavingReceivedAndHandledIntegrationEventAsync(GridAreaOwnershipAssigned.EventName, gridAreaOwnershipAssignedEvent02);

        var gridAreas = await GetGridAreas();
        var gridArea = gridAreas.Single();
        Assert.Equal(gridAreaOwner2, gridArea.GridAreaOwnerActorNumber);
    }

    private async Task HavingReceivedAndHandledIntegrationEventAsync(string eventType, GridAreaOwnershipAssigned gridAreaOwnershipAssigned)
    {
        var integrationEventHandler = GetService<IIntegrationEventHandler>();

        var integrationEvent = new IntegrationEvent(Guid.NewGuid(), eventType, 1, gridAreaOwnershipAssigned);

        await integrationEventHandler.HandleAsync(integrationEvent).ConfigureAwait(false);
    }

    private async Task<IEnumerable<GridArea>> GetGridAreas()
    {
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = $"SELECT GridAreaCode, GridAreaOwnerActorNumber FROM [dbo].[GridArea]";
        return await connection.QueryAsync<GridArea>(sql);
    }

#pragma warning disable
    public record GridArea(string GridAreaCode, string GridAreaOwnerActorNumber);
#pragma warning restore
}
