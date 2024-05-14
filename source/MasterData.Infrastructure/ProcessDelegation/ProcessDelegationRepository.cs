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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.MasterData.Domain.ProcessDelegations;
using Energinet.DataHub.EDI.MasterData.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Energinet.DataHub.EDI.MasterData.Infrastructure.ProcessDelegation;

public class ProcessDelegationRepository : IProcessDelegationRepository
{
    private readonly MasterDataContext _masterDataContext;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;

    public ProcessDelegationRepository(MasterDataContext masterDataContext, ISystemDateTimeProvider systemDateTimeProvider)
    {
        _masterDataContext = masterDataContext;
        _systemDateTimeProvider = systemDateTimeProvider;
    }

    public void Create(Domain.ProcessDelegations.ProcessDelegation processDelegation, CancellationToken cancellationToken)
    {
        _masterDataContext.ProcessDelegations.Add(processDelegation);
    }

    public async Task<Domain.ProcessDelegations.ProcessDelegation?> GetProcessesDelegatedByAsync(
        ActorNumber delegatedByActorNumber,
        ActorRole delegatedByActorRole,
        string gridAreaCode,
        ProcessType processType,
        CancellationToken cancellationToken)
    {
        var now = _systemDateTimeProvider.Now();

        var query = GetBaseDelegationQuery(
            now,
            delegatedBy: new(delegatedByActorNumber, delegatedByActorRole),
            delegatedTo: null,
            processType);

        var latestDelegation = await query
            .Where(pd => pd.GridAreaCode == gridAreaCode)
            .OrderByDescending(pd => pd.SequenceNumber)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (latestDelegation == null)
            return null;

        if (latestDelegation.StopsAt <= now)
            return null;

        return latestDelegation;
    }

    public async Task<IReadOnlyCollection<Domain.ProcessDelegations.ProcessDelegation>> GetProcessesDelegatedToAsync(
        ActorNumber delegatedToActorNumber,
        ActorRole delegatedToActorRole,
        string? gridAreaCode,
        ProcessType processType,
        CancellationToken cancellationToken)
    {
        var now = _systemDateTimeProvider.Now();

        var query = GetBaseDelegationQuery(
            now,
            delegatedBy: null,
            delegatedTo: new(delegatedToActorNumber, delegatedToActorRole),
            processType);

        if (!string.IsNullOrEmpty(gridAreaCode))
            query = query.Where(pd => pd.GridAreaCode == gridAreaCode);

        // Get result grouped by each grid area code, so we can get the latest delegation for each grid area
        var result = await query
            .GroupBy(pd => pd.GridAreaCode)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Get the current delegation for each grid area
        var latestForGridAreas = result
            .Select(group => group
                .OrderByDescending(pd => pd.SequenceNumber)
                .First())
            .Where(d => d.StopsAt > now)
            .ToList();

        return latestForGridAreas;
    }

    private IQueryable<Domain.ProcessDelegations.ProcessDelegation> GetBaseDelegationQuery(
        Instant now,
        Actor? delegatedBy,
        Actor? delegatedTo,
        ProcessType processType)
    {
        if (delegatedBy == null && delegatedTo == null) throw new ArgumentException("At least one of the delegatedBy or delegatedTo must be set");
        if (delegatedBy != null && delegatedTo != null) throw new ArgumentException("Only one of the delegatedBy or delegatedTo must be set");

        // The latest delegation can cover the period from the start date to the end date.
        // If a delegation relationship has been cancelled the EndsAt is set to StartsAt.
        // Therefore, we can not use the EndsAt to determine if the delegation is active in the query.
        var delegationQuery = Queryable
            .Where<Domain.ProcessDelegations.ProcessDelegation>(
                _masterDataContext.ProcessDelegations,
                pd => pd.DelegatedProcess == processType
                      && pd.StartsAt <= now);

        if (delegatedBy != null)
        {
            delegationQuery = delegationQuery.Where(pd =>
                    pd.DelegatedByActorNumber == delegatedBy.ActorNumber
                    && pd.DelegatedByActorRole == delegatedBy.ActorRole);
        }

        if (delegatedTo != null)
        {
            delegationQuery = delegationQuery.Where(pd =>
                    pd.DelegatedToActorNumber == delegatedTo.ActorNumber
                    && pd.DelegatedToActorRole == delegatedTo.ActorRole);
        }

        return delegationQuery;
    }
}
