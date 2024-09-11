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
using Energinet.DataHub.EDI.MasterData.Domain.GridAreaOwners;
using Energinet.DataHub.EDI.MasterData.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Energinet.DataHub.EDI.MasterData.Infrastructure.GridAreas;

public class GridAreaRepository : IGridAreaRepository
{
    private readonly MasterDataContext _masterDataContext;
    private readonly IClock _clock;

    public GridAreaRepository(MasterDataContext masterDataContext, IClock clock)
    {
        _masterDataContext = masterDataContext;
        _clock = clock;
    }

    public async Task UpdateOwnershipAsync(
        string gridAreaCode,
        Instant validFrom,
        ActorNumber actorNumber,
        int sequenceNumber,
        CancellationToken cancellationToken)
    {
        await _masterDataContext.GridAreaOwners
            .AddAsync(new GridAreaOwner(gridAreaCode, validFrom, actorNumber, sequenceNumber), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<GridAreaOwner?> GetGridOwnerAsync(string gridAreaCode, CancellationToken cancellationToken)
    {
        var now = _clock.GetCurrentInstant();
        return await _masterDataContext.GridAreaOwners
            .Where(gridArea => gridArea.GridAreaCode == gridAreaCode && gridArea.ValidFrom <= now)
            .OrderByDescending(gridArea => gridArea.SequenceNumber)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }
}
