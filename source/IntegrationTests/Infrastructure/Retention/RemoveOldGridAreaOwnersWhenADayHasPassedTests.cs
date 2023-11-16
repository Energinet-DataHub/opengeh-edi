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
using Microsoft.EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Retention;

public class RemoveOldGridAreaOwnersWhenADayHasPassedTests : TestBase
{
    private readonly B2BContext _b2bContext;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly GridAreaOwnerRetention _sut;

    public RemoveOldGridAreaOwnersWhenADayHasPassedTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _b2bContext = GetService<B2BContext>();
        _systemDateTimeProvider = GetService<ISystemDateTimeProvider>();
        _sut = new GridAreaOwnerRetention(
            _systemDateTimeProvider,
            _b2bContext);
    }

    [Fact]
    public async Task Clean_up_grid_area_owners_succeed()
    {
        // Arrange
        var gridAreaCode = "801";
        await AddGridAreaOwnersForGridArea(gridAreaCode, 3);

        // Act
        await _sut.CleanupAsync(CancellationToken.None);

        // Assert
        await AssertCountOfGridAreaOwnersAsync(gridAreaCode, 1);
    }

    [Fact]
    public async Task Clean_up_grid_area_owners_with_multiple_owners_succeed()
    {
        // Arrange
        var gridAreaCode1 = "801";
        var gridAreaCode2 = "802";
        var gridAreaCode3 = "803";
        await AddGridAreaOwnersForGridArea(gridAreaCode1, 3);
        await AddGridAreaOwnersForGridArea(gridAreaCode2, 2);
        await AddGridAreaOwnersForGridArea(gridAreaCode3, 1);

        // Act
        await _sut.CleanupAsync(CancellationToken.None);

        // Assert
        await AssertCountOfGridAreaOwnersAsync(gridAreaCode1, 1);
        await AssertCountOfGridAreaOwnersAsync(gridAreaCode2, 1);
        await AssertCountOfGridAreaOwnersAsync(gridAreaCode3, 1);
    }

    protected override void Dispose(bool disposing)
    {
        _b2bContext.Dispose();
        base.Dispose(disposing);
    }

    private static string RandomStringOfLength13()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 13)
#pragma warning disable CA5394
            .Select(s => s[random.Next(s.Length)]).ToArray());
#pragma warning restore CA5394
    }

    private async Task AddGridAreaOwnersForGridArea(string gridAreaCode, int amountOfGridAreaOwners)
    {
        var now = _systemDateTimeProvider.Now();
        var gridAreaOwners = Enumerable.Range(0, amountOfGridAreaOwners)
            .Select(i => new GridAreaOwner(
                gridAreaCode,
                now.PlusDays(-i - 30).PlusMinutes(-4),
                ActorNumber.Create(RandomStringOfLength13()),
                amountOfGridAreaOwners - i));
        await _b2bContext.GridAreaOwners.AddRangeAsync(gridAreaOwners);
        await _b2bContext.SaveChangesAsync();
    }

    private async Task AssertCountOfGridAreaOwnersAsync(string gridArea, int count)
    {
        var gridAreaOwners = await _b2bContext.GridAreaOwners
            .Where(x => x.GridAreaCode == gridArea)
            .ToListAsync();
        Assert.Equal(count, gridAreaOwners.Count);
    }
}
