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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Common.Actors;
using Energinet.DataHub.EDI.Common.DateTime;
using Energinet.DataHub.EDI.Domain.GridAreaOwners;
using Energinet.DataHub.EDI.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Infrastructure.GridAreas;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Retention;

public class RemoveOldGridAreaOwnersWhenADayHasPassedTests : TestBase
{
    private readonly B2BContext _b2bContext;

    public RemoveOldGridAreaOwnersWhenADayHasPassedTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _b2bContext = GetService<B2BContext>();
    }

    [Fact]
    public async Task Clean_up_grid_area_owners__has_single_owner()
    {
        // Arrange
        var actorNumberOfExpectedOwner = ActorNumber.Create("9876543210987");
        var gridAreaCode = "303";
        var gridAreaOwner1 = new GridAreaOwner(gridAreaCode, Instant.FromUtc(2023, 10, 1, 0, 0, 0), ActorNumber.Create("1234567890123"), 1);
        var gridAreaOwner2 = new GridAreaOwner(gridAreaCode, Instant.FromUtc(2023, 10, 2, 0, 0, 0), actorNumberOfExpectedOwner, 2);

        await AddActorsToDatabaseAsync(new List<GridAreaOwner>() { gridAreaOwner1, gridAreaOwner2 });

        var sut = new GridAreaOwnerRetention(new SystemProviderMock(Instant.FromUtc(2023, 11, 3, 0, 0, 0)), _b2bContext);

        // Act
        await sut.CleanupAsync(CancellationToken.None);

        // Assert
        var owners = await GetGridAreaOwnersForGridArea(gridAreaCode);
        Assert.Single(owners);
        Assert.Equal(gridAreaOwner2.GridAreaOwnerActorNumber.Value, owners.First().GridAreaOwnerActorNumber.Value);
    }

    [Fact]
    public async Task Clean_up_grid_area_owners__does_not_delete_non_expired_grid_owners()
    {
        // Arrange
        var actorNumberOfExpectedOwner = ActorNumber.Create("9876543210987");
        var gridAreaCode = "303";
        var gridAreaOwner1 = new GridAreaOwner(gridAreaCode, Instant.FromUtc(2023, 10, 4, 0, 0, 0), ActorNumber.Create("1234567890123"), 1);
        var gridAreaOwner2 = new GridAreaOwner(gridAreaCode, Instant.FromUtc(2023, 10, 5, 0, 0, 0), actorNumberOfExpectedOwner, 2);

        await AddActorsToDatabaseAsync(new List<GridAreaOwner>() { gridAreaOwner1, gridAreaOwner2 });

        var sut = new GridAreaOwnerRetention(new SystemProviderMock(Instant.FromUtc(2023, 11, 3, 0, 0, 0)), _b2bContext);

        // Act
        await sut.CleanupAsync(CancellationToken.None);

        // Assert
        var owners = await GetGridAreaOwnersForGridArea(gridAreaCode);
        Assert.Equal(2, owners.Count);
    }

    [Fact]
    public async Task Clean_up_grid_area_owners_with_multiple_owners__all_grid_areas_has_owner()
    {
        // Arrange
        var gridAreaCode1 = "301";
        var gridAreaOwner1 = new GridAreaOwner(gridAreaCode1, Instant.FromUtc(2023, 10, 1, 0, 0, 0), ActorNumber.Create("1234567890123"), 1);
        var gridAreaOwner2 = new GridAreaOwner(gridAreaCode1, Instant.FromUtc(2023, 10, 2, 0, 0, 0), ActorNumber.Create("9876543210987"), 2);
        await AddActorsToDatabaseAsync(new List<GridAreaOwner>() { gridAreaOwner1, gridAreaOwner2 });

        var gridAreaCode2 = "302";
        var gridAreaOwner3 = new GridAreaOwner(gridAreaCode2, Instant.FromUtc(2023, 10, 1, 0, 0, 0), ActorNumber.Create("1234567890123"), 1);
        var gridAreaOwner4 = new GridAreaOwner(gridAreaCode2, Instant.FromUtc(2023, 10, 2, 0, 0, 0), ActorNumber.Create("9876543210987"), 2);
        await AddActorsToDatabaseAsync(new List<GridAreaOwner>() { gridAreaOwner3, gridAreaOwner4 });

        var sut = new GridAreaOwnerRetention(new SystemProviderMock(Instant.FromUtc(2023, 11, 3, 0, 0, 0)), _b2bContext);

        // Act
        await sut.CleanupAsync(CancellationToken.None);

        // Assert
        Assert.Single(await GetGridAreaOwnersForGridArea(gridAreaCode1));
        Assert.Single(await GetGridAreaOwnersForGridArea(gridAreaCode2));
    }

    protected override void Dispose(bool disposing)
    {
        _b2bContext.Dispose();
        base.Dispose(disposing);
    }

    private async Task AddActorsToDatabaseAsync(List<GridAreaOwner> gridAreaOwners)
    {
        await _b2bContext.GridAreaOwners.AddRangeAsync(gridAreaOwners);
        await _b2bContext.SaveChangesAsync();
    }

    private async Task<List<GridAreaOwner>> GetGridAreaOwnersForGridArea(string gridAreaCode)
    {
        return await _b2bContext.GridAreaOwners
            .Where(x => x.GridAreaCode == gridAreaCode)
            .ToListAsync();
    }

    private sealed class SystemProviderMock : ISystemDateTimeProvider
    {
        private readonly Instant _now;

        public SystemProviderMock(Instant now)
        {
            _now = now;
        }

        public Instant Now() => _now;
    }
}
