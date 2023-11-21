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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Application.GridAreas;
using Energinet.DataHub.EDI.Common.Actors;
using Energinet.DataHub.EDI.Common.DateTime;
using Energinet.DataHub.EDI.Domain.GridAreaOwners;
using Energinet.DataHub.EDI.Infrastructure.Configuration.DataAccess;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Energinet.DataHub.EDI.Infrastructure.GridAreas;

public class GridAreaRepository : IGridAreaRepository
{
    private readonly B2BContext _dbContext;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;

    public GridAreaRepository(B2BContext dbContext, ISystemDateTimeProvider systemDateTimeProvider)
    {
        _dbContext = dbContext;
        _systemDateTimeProvider = systemDateTimeProvider;
    }

    public async Task UpdateOwnershipAsync(
        string gridAreaCode,
        Instant validFrom,
        ActorNumber actorNumber,
        int sequenceNumber,
        CancellationToken cancellationToken)
    {
        await _dbContext.GridAreaOwners.AddAsync(new GridAreaOwner(gridAreaCode, validFrom, actorNumber, sequenceNumber), cancellationToken).ConfigureAwait(false);
    }

    public async Task<ActorNumber> GetGridOwnerForAsync(string gridAreaCode, CancellationToken cancellationToken)
    {
        var now = _systemDateTimeProvider.Now();
        var gridAreaOwner = await _dbContext.GridAreaOwners
            .Where(gridArea => gridArea.GridAreaCode == gridAreaCode && gridArea.ValidFrom <= now)
            .OrderByDescending(gridArea => gridArea.SequenceNumber)
            .FirstAsync(cancellationToken).ConfigureAwait(false);
        return gridAreaOwner.GridAreaOwnerActorNumber;
    }
}
