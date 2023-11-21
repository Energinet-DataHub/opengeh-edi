﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.EDI.Common.Actors;
using Energinet.DataHub.EDI.Common.DataAccess;
using Energinet.DataHub.EDI.Common.DateTime;
using Energinet.DataHub.EDI.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Infrastructure.GridAreas;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Contracts;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
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
    public async Task New_grid_area_ownership_assigned_event_is_received_stores_grid_area_owner()
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
    public async Task New_grid_area_event_for_existing_grid_area_code_with_valid_from_in_future_does_not_update_owner()
    {
        var futureValidFrom = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(1));
        const string owner = "1234567890123";
        var gridAreaOwnershipAssignedEvent01 = _gridAreaOwnershipAssignedEventBuilder
            .WithOwnerShipActorNumber(owner)
            .WithSequenceNumber(1)
            .Build();
        var gridAreaOwnershipAssignedEvent02 = _gridAreaOwnershipAssignedEventBuilder
            .WithOwnerShipActorNumber("9876543210987")
            .WithValidFrom(futureValidFrom)
            .WithSequenceNumber(2)
            .Build();

        await HavingReceivedAndHandledIntegrationEventAsync(GridAreaOwnershipAssigned.EventName, gridAreaOwnershipAssignedEvent01);
        await HavingReceivedAndHandledIntegrationEventAsync(GridAreaOwnershipAssigned.EventName, gridAreaOwnershipAssignedEvent02);

        var gridAreas = await GetGridAreas();
        Assert.True(2 == gridAreas.Count());
        var actorNumber = await GetOwner(gridAreaOwnershipAssignedEvent01.GridAreaCode);
        Assert.Equal(owner, actorNumber.Value);
    }

    [Fact]
    public async Task New_grid_area_event_for_existing_grid_area_code_with_valid_from_in_past_and_higher_sequence_number_updates_owner()
    {
        var olderValidFrom = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(-1));
        const string owner = "1234567890123";
        var gridAreaOwnershipAssignedEvent01 = _gridAreaOwnershipAssignedEventBuilder
            .WithOwnerShipActorNumber("9876543210987")
            .WithSequenceNumber(1)
            .Build();
        var gridAreaOwnershipAssignedEvent02 = _gridAreaOwnershipAssignedEventBuilder
            .WithSequenceNumber(2)
            .WithValidFrom(olderValidFrom)
            .WithOwnerShipActorNumber(owner)
            .Build();

        await HavingReceivedAndHandledIntegrationEventAsync(GridAreaOwnershipAssigned.EventName, gridAreaOwnershipAssignedEvent01);
        await HavingReceivedAndHandledIntegrationEventAsync(GridAreaOwnershipAssigned.EventName, gridAreaOwnershipAssignedEvent02);

        var actorNumber = await GetOwner(gridAreaOwnershipAssignedEvent01.GridAreaCode);
        Assert.Equal(owner, actorNumber.Value);
    }

    [Fact]
    public async Task Multiple_grid_area_ownership_events_with_same_valid_from_highest_sequence_number_is_owner()
    {
        const string owner = "9876543210987";
        var validFrom = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(-4));
        var gridAreaOwnershipAssignedEvent01 = _gridAreaOwnershipAssignedEventBuilder
            .WithValidFrom(validFrom)
            .WithSequenceNumber(1)
            .Build();
        var gridAreaOwnershipAssignedEvent02 = _gridAreaOwnershipAssignedEventBuilder
            .WithOwnerShipActorNumber(owner)
            .WithValidFrom(validFrom)
            .WithSequenceNumber(2)
            .Build();

        await HavingReceivedAndHandledIntegrationEventAsync(GridAreaOwnershipAssigned.EventName, gridAreaOwnershipAssignedEvent01);
        await HavingReceivedAndHandledIntegrationEventAsync(GridAreaOwnershipAssigned.EventName, gridAreaOwnershipAssignedEvent02);

        var actorNumber = await GetOwner(gridAreaOwnershipAssignedEvent01.GridAreaCode);
        Assert.Equal(owner, actorNumber.Value);
    }

    [Fact]
    public async Task Multiple_grid_area_ownership_events_with_same_valid_from_highest_sequence_number_is_owner_reversed_order()
    {
        const string gridAreaOwner1 = "9876543210987";
        const string owner = "1234567890123";
        var validFrom = Timestamp.FromDateTime(DateTime.UtcNow.AddMinutes(-4));
        var gridAreaOwnershipAssignedEvent01 = _gridAreaOwnershipAssignedEventBuilder
            .WithOwnerShipActorNumber(owner)
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

        var actorNumber = await GetOwner(gridAreaOwnershipAssignedEvent01.GridAreaCode);
        Assert.Equal(owner, actorNumber.Value);
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
        var sql = $"SELECT GridAreaCode, GridAreaOwnerActorNumber FROM [dbo].[GridAreaOwner]";
        return await connection.QueryAsync<GridArea>(sql);
    }

    private async Task<ActorNumber> GetOwner(string gridAreaCode)
    {
        var serviceScopeFactory = GetService<IServiceScopeFactory>();
        using var newScope = serviceScopeFactory.CreateScope();
        var context = newScope.ServiceProvider.GetRequiredService<B2BContext>();
        var systemTime = newScope.ServiceProvider.GetRequiredService<ISystemDateTimeProvider>();
        var gridAreaRepository = new GridAreaRepository(context, systemTime);
        return await gridAreaRepository.GetGridOwnerForAsync(gridAreaCode, CancellationToken.None).ConfigureAwait(false);
    }

#pragma warning disable
    public record GridArea(string GridAreaCode, string GridAreaOwnerActorNumber);
#pragma warning restore
}
