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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.MasterData.IntegrationTests.Builders;
using Energinet.DataHub.EDI.MasterData.IntegrationTests.Fixture;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Xunit;
using Xunit.Abstractions;
using Duration = NodaTime.Duration;

namespace Energinet.DataHub.EDI.MasterData.IntegrationTests.Tests;

[Collection(nameof(MasterDataTestCollection))]
public class GridAreaOwnershipTests : MasterDataTestBase
{
    private readonly GridAreaOwnershipAssignedDtoBuilder _gridAreaOwnershipAssignedDtoBuilder;
    private readonly IMasterDataClient _client;

    public GridAreaOwnershipTests(MasterDataFixture masterDataFixture, ITestOutputHelper testOutputHelper)
        : base(masterDataFixture, testOutputHelper)
    {
        SetupServiceCollection();
        _gridAreaOwnershipAssignedDtoBuilder = new GridAreaOwnershipAssignedDtoBuilder();
        _client = Services.GetRequiredService<IMasterDataClient>();
    }

    [Fact]
    public async Task WhenMultipleGridAreaAssignedDtoIsReceived_ForSameActor_GridAreaOwnerIsUpdatedForTheSameActor()
    {
        var gridAreaOwnershipAssignedEvent01 = _gridAreaOwnershipAssignedDtoBuilder
            .WithGridAreaCode("543")
            .Build();
        var gridAreaOwnershipAssignedEvent02 = _gridAreaOwnershipAssignedDtoBuilder
            .WithGridAreaCode("804")
            .Build();

        await _client.UpdateGridAreaOwnershipAsync(gridAreaOwnershipAssignedEvent01, CancellationToken.None);
        await _client.UpdateGridAreaOwnershipAsync(gridAreaOwnershipAssignedEvent02, CancellationToken.None);

        var gridAreas = await GetGridAreas();
        Assert.All(gridAreas, area => Assert.Equal(area.GridAreaOwnerActorNumber, gridAreaOwnershipAssignedEvent01.GridAreaOwner.Value));
    }

    [Fact]
    public async Task WhenGridAreaAssignedDtoIsReceived_WithValidFromInTheFuture_GridAreaOwnerIsNotUpdated()
    {
        var futureValidFrom = SystemClock.Instance.GetCurrentInstant().Plus(Duration.FromDays(1));
        var owner = ActorNumber.Create("1234567890123");
        var gridAreaOwnershipAssignedEvent01 = _gridAreaOwnershipAssignedDtoBuilder
            .WithOwnerShipActorNumber(owner)
            .WithSequenceNumber(1)
            .Build();
        var gridAreaOwnershipAssignedEvent02 = _gridAreaOwnershipAssignedDtoBuilder
            .WithOwnerShipActorNumber(ActorNumber.Create("9876543210987"))
            .WithValidFrom(futureValidFrom)
            .WithSequenceNumber(2)
            .Build();

        await _client.UpdateGridAreaOwnershipAsync(gridAreaOwnershipAssignedEvent01, CancellationToken.None);
        await _client.UpdateGridAreaOwnershipAsync(gridAreaOwnershipAssignedEvent02, CancellationToken.None);

        var gridAreas = await GetGridAreas();
        Assert.True(2 == gridAreas.Count());
        var actorNumber = await GetOwner(gridAreaOwnershipAssignedEvent01.GridAreaCode);
        Assert.Equal(owner.Value, actorNumber.Value);
    }

    [Fact]
    public async Task WhenGridAreaAssignedDtoIsReceived_WithValidFromInThePast_GridAreaOwnerIsUpdated()
    {
        var olderValidFrom = SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(1));
        var owner = ActorNumber.Create("1234567890123");
        var gridAreaOwnershipAssignedEvent01 = _gridAreaOwnershipAssignedDtoBuilder
            .WithOwnerShipActorNumber(ActorNumber.Create("9876543210987"))
            .WithSequenceNumber(1)
            .Build();
        var gridAreaOwnershipAssignedEvent02 = _gridAreaOwnershipAssignedDtoBuilder
            .WithSequenceNumber(2)
            .WithValidFrom(olderValidFrom)
            .WithOwnerShipActorNumber(owner)
            .Build();

        await _client.UpdateGridAreaOwnershipAsync(gridAreaOwnershipAssignedEvent01, CancellationToken.None);
        await _client.UpdateGridAreaOwnershipAsync(gridAreaOwnershipAssignedEvent02, CancellationToken.None);

        var actorNumber = await GetOwner(gridAreaOwnershipAssignedEvent01.GridAreaCode);
        Assert.Equal(owner.Value, actorNumber.Value);
    }

    [Fact]
    public async Task WhenGridAreaAssignedDtoIsReceived_WithSameValidFromButDifferentSequenceNumber_GridAreaOwnerIsUpdatedForHighestSequenceNumber()
    {
        var owner = ActorNumber.Create("9876543210987");
        var validFrom = SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromMinutes(4));
        var gridAreaOwnershipAssignedEvent01 = _gridAreaOwnershipAssignedDtoBuilder
            .WithValidFrom(validFrom)
            .WithSequenceNumber(1)
            .Build();
        var gridAreaOwnershipAssignedEvent02 = _gridAreaOwnershipAssignedDtoBuilder
            .WithOwnerShipActorNumber(owner)
            .WithValidFrom(validFrom)
            .WithSequenceNumber(2)
            .Build();

        await _client.UpdateGridAreaOwnershipAsync(gridAreaOwnershipAssignedEvent01, CancellationToken.None);
        await _client.UpdateGridAreaOwnershipAsync(gridAreaOwnershipAssignedEvent02, CancellationToken.None);

        var actorNumber = await GetOwner(gridAreaOwnershipAssignedEvent01.GridAreaCode);
        Assert.Equal(owner.Value, actorNumber.Value);
    }

    [Fact]
    public async Task WhenGridAreaAssignedDtoIsReceived_WithSameValidFromButDifferentSequenceNumber_GridAreaOwnerIsUpdatedForHighestSequenceNumberInReversedOrder()
    {
        var gridAreaOwner1 = ActorNumber.Create("9876543210987");
        var owner = ActorNumber.Create("1234567890123");
        var validFrom = SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromMinutes(4));
        var gridAreaOwnershipAssignedEvent01 = _gridAreaOwnershipAssignedDtoBuilder
            .WithOwnerShipActorNumber(owner)
            .WithValidFrom(validFrom)
            .WithSequenceNumber(2)
            .Build();
        var gridAreaOwnershipAssignedEvent02 = _gridAreaOwnershipAssignedDtoBuilder
            .WithOwnerShipActorNumber(gridAreaOwner1)
            .WithValidFrom(validFrom)
            .WithSequenceNumber(1)
            .Build();

        await _client.UpdateGridAreaOwnershipAsync(gridAreaOwnershipAssignedEvent01, CancellationToken.None);
        await _client.UpdateGridAreaOwnershipAsync(gridAreaOwnershipAssignedEvent02, CancellationToken.None);

        var actorNumber = await GetOwner(gridAreaOwnershipAssignedEvent01.GridAreaCode);
        Assert.Equal(owner.Value, actorNumber.Value);
    }

    private async Task<IEnumerable<GridArea>> GetGridAreas()
    {
        var connectionFactory = Services.GetService<IDatabaseConnectionFactory>();
        using var connection = await connectionFactory!.GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = $"SELECT GridAreaCode, GridAreaOwnerActorNumber FROM [dbo].[GridAreaOwner]";
        return await connection.QueryAsync<GridArea>(sql);
    }

    private async Task<ActorNumber> GetOwner(string gridAreaCode)
    {
        var serviceScopeFactory = Services.GetService<IServiceScopeFactory>();
        using var newScope = serviceScopeFactory!.CreateScope();
        var masterDataClient = newScope.ServiceProvider.GetRequiredService<IMasterDataClient>();
        var gridAreaOwner = await masterDataClient
            .GetGridOwnerForGridAreaCodeAsync(gridAreaCode, CancellationToken.None)
            .ConfigureAwait(false);
        return gridAreaOwner.ActorNumber;
    }

#pragma warning disable
    public record GridArea(string GridAreaCode, string GridAreaOwnerActorNumber);
#pragma warning restore
}
