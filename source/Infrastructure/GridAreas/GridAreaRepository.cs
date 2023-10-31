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

using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Application.GridAreas;
using Energinet.DataHub.EDI.Common.Actors;
using Energinet.DataHub.EDI.Domain.GridAreas;
using Energinet.DataHub.EDI.Infrastructure.Configuration.DataAccess;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Energinet.DataHub.EDI.Infrastructure.GridAreas;

public class GridAreaRepository : IGridAreaRepository
{
    private readonly B2BContext _dbContext;

    public GridAreaRepository(B2BContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task UpdateOwnershipAsync(
        string gridAreaCode,
        Instant validFrom,
        ActorNumber actorNumber,
        CancellationToken cancellationToken)
    {
        var gridArea = await GetGridAreaAsync(gridAreaCode, cancellationToken).ConfigureAwait(false);
        if (gridArea == null)
            await _dbContext.GridAreas.AddAsync(new GridArea(gridAreaCode, validFrom, actorNumber), cancellationToken).ConfigureAwait(false);
        else
            gridArea.UpdateOwnership(validFrom, actorNumber);
    }

    public async Task<ActorNumber> GetGridOwnerForAsync(string gridAreaCode, CancellationToken cancellationToken)
    {
        var gridAreaByCode = await _dbContext.GridAreas
            .FirstAsync(
                gridArea => gridArea.GridAreaCode == gridAreaCode,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return gridAreaByCode.GridAreaOwnerActorNumber;
    }

    private async Task<GridArea?> GetGridAreaAsync(
        string gridAreaCode,
        CancellationToken cancellationToken)
    {
        return await _dbContext.GridAreas
            .FirstOrDefaultAsync(
                gridArea => gridArea.GridAreaCode == gridAreaCode,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }
}
