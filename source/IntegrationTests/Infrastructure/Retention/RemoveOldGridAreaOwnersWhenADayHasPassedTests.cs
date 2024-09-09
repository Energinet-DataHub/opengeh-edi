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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.MasterData.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Retention;

public class RemoveOldGridAreaOwnersWhenADayHasPassedTests : TestBase
{
    private readonly IMasterDataClient _masterDataClient;

    public RemoveOldGridAreaOwnersWhenADayHasPassedTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _masterDataClient = GetService<IMasterDataClient>();
    }

    [Fact]
    public async Task Clean_up_grid_area_owners__has_single_owner()
    {
        // Arrange
        var actorNumberOfExpectedOwner = ActorNumber.Create("9876543210987");
        var gridAreaCode = "303";
        var gridAreaOwner1 = new GridAreaOwnershipAssignedDto(
            gridAreaCode,
            Instant.FromUtc(2023, 10, 1, 0, 0, 0),
            ActorNumber.Create("1234567890123"),
            1);

        var gridAreaOwner2 = new GridAreaOwnershipAssignedDto(
            gridAreaCode,
            Instant.FromUtc(2023, 10, 2, 0, 0, 0),
            actorNumberOfExpectedOwner,
            2);

        await AddActorsToDatabaseAsync(new List<GridAreaOwnershipAssignedDto> { gridAreaOwner1, gridAreaOwner2 });

        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var scopedServiceCollection = new ServiceCollection();
        scopedServiceCollection.AddMasterDataModule(config);
        scopedServiceCollection.AddScoped<IClock>(
            _ => new ClockStub(Instant.FromUtc(2023, 11, 3, 0, 0, 0)));

        var sut = scopedServiceCollection.BuildServiceProvider().GetService<IDataRetention>()
                  ?? throw new ArgumentNullException();

        // Act
        await sut.CleanupAsync(CancellationToken.None);

        // Assert
        var owners = await GetGridAreaOwnersForGridArea(gridAreaCode);
        Assert.NotNull(owners);
        Assert.Equal(gridAreaOwner2.GridAreaOwner.Value, owners.Value);
    }

    [Fact]
    public async Task Clean_up_grid_area_owners__does_not_delete_non_expired_grid_owners()
    {
        // Arrange
        var actorNumberOfExpectedOwner = ActorNumber.Create("9876543210987");
        var gridAreaCode = "303";
        var gridAreaOwner1 = new GridAreaOwnershipAssignedDto(
            gridAreaCode,
            Instant.FromUtc(2023, 10, 4, 0, 0, 0),
            ActorNumber.Create("1234567890123"),
            1);

        var gridAreaOwner2 = new GridAreaOwnershipAssignedDto(
            gridAreaCode,
            Instant.FromUtc(2023, 10, 5, 0, 0, 0),
            actorNumberOfExpectedOwner,
            2);

        await AddActorsToDatabaseAsync(new List<GridAreaOwnershipAssignedDto> { gridAreaOwner1, gridAreaOwner2 });

        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var scopedServiceCollection = new ServiceCollection();
        scopedServiceCollection.AddMasterDataModule(config);
        scopedServiceCollection.AddScoped<IClock>(
            _ => new ClockStub(Instant.FromUtc(2023, 11, 3, 0, 0, 0)));

        var sut = scopedServiceCollection.BuildServiceProvider().GetService<IDataRetention>()
                  ?? throw new ArgumentNullException();

        // Act
        await sut.CleanupAsync(CancellationToken.None);

        // Assert
        var owners = await GetGridAreaOwnersForGridArea(gridAreaCode);
        // Assert.Equal(2, owners.Count);
    }

    [Fact]
    public async Task Clean_up_grid_area_owners_with_multiple_owners__all_grid_areas_has_owner()
    {
        // Arrange
        var gridAreaCode1 = "301";
        var gridAreaOwner1 = new GridAreaOwnershipAssignedDto(
            gridAreaCode1,
            Instant.FromUtc(2023, 10, 1, 0, 0, 0),
            ActorNumber.Create("1234567890123"),
            1);

        var gridAreaOwner2 = new GridAreaOwnershipAssignedDto(
            gridAreaCode1,
            Instant.FromUtc(2023, 10, 2, 0, 0, 0),
            ActorNumber.Create("9876543210987"),
            2);

        await AddActorsToDatabaseAsync(new List<GridAreaOwnershipAssignedDto> { gridAreaOwner1, gridAreaOwner2 });

        var gridAreaCode2 = "302";
        var gridAreaOwner3 = new GridAreaOwnershipAssignedDto(
            gridAreaCode2,
            Instant.FromUtc(2023, 10, 1, 0, 0, 0),
            ActorNumber.Create("1234567890123"),
            1);

        var gridAreaOwner4 = new GridAreaOwnershipAssignedDto(
            gridAreaCode2,
            Instant.FromUtc(2023, 10, 2, 0, 0, 0),
            ActorNumber.Create("9876543210987"),
            2);

        await AddActorsToDatabaseAsync(new List<GridAreaOwnershipAssignedDto> { gridAreaOwner3, gridAreaOwner4 });

        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var scopedServiceCollection = new ServiceCollection();
        scopedServiceCollection.AddMasterDataModule(config);
        scopedServiceCollection.AddScoped<IClock>(
            _ => new ClockStub(Instant.FromUtc(2023, 11, 3, 0, 0, 0)));

        var sut = scopedServiceCollection.BuildServiceProvider().GetService<IDataRetention>()
                  ?? throw new ArgumentNullException();

        // Act
        await sut.CleanupAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(await GetGridAreaOwnersForGridArea(gridAreaCode1));
        Assert.NotNull(await GetGridAreaOwnersForGridArea(gridAreaCode2));
    }

    private async Task AddActorsToDatabaseAsync(List<GridAreaOwnershipAssignedDto> gridAreaOwners)
    {
        foreach (var gao in gridAreaOwners)
            await _masterDataClient.UpdateGridAreaOwnershipAsync(gao, CancellationToken.None);
    }

    private async Task<ActorNumber> GetGridAreaOwnersForGridArea(string gridAreaCode)
    {
        return await _masterDataClient.GetGridOwnerForGridAreaCodeAsync(gridAreaCode, CancellationToken.None);
    }
}
