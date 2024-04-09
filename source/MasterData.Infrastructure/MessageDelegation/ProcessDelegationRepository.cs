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
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.EDI.MasterData.Infrastructure.MessageDelegation;

public class ProcessDelegationRepository : IProcessDelegationRepository
{
    private readonly MasterDataContext _masterDataContext;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;

    public ProcessDelegationRepository(MasterDataContext masterDataContext, ISystemDateTimeProvider systemDateTimeProvider)
    {
        _masterDataContext = masterDataContext;
        _systemDateTimeProvider = systemDateTimeProvider;
    }

    public void Create(ProcessDelegation processDelegation, CancellationToken cancellationToken)
    {
        _masterDataContext.ProcessDelegations.Add(processDelegation);
    }

    public Task<ProcessDelegation?> GetProcessesDelegatedByAsync(
        ActorNumber delegatedByActorNumber,
        ActorRole delegatedByActorRole,
        string gridAreaCode,
        ProcessType processType,
        CancellationToken cancellationToken)
    {
        var query = GetBaseDelegationQuery(
            delegatedBy: new ActorNumberAndRoleDto(delegatedByActorNumber, delegatedByActorRole),
            delegatedTo: null,
            processType);

        return query
            .Where(pd => pd.GridAreaCode == gridAreaCode)
            .OrderByDescending(pd => pd.SequenceNumber) // TODO: Shouldn't this order by start date?
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProcessDelegation>> GetProcessesDelegatedToAsync(
        ActorNumber delegatedToActorNumber,
        ActorRole delegatedToActorRole,
        string? gridAreaCode,
        ProcessType processType,
        CancellationToken cancellationToken)
    {
        var query = GetBaseDelegationQuery(
            delegatedBy: null,
            delegatedTo: new ActorNumberAndRoleDto(delegatedToActorNumber, delegatedToActorRole),
            processType);

        if (!string.IsNullOrEmpty(gridAreaCode))
            query = query.Where(pd => pd.GridAreaCode == gridAreaCode);

        // Get result grouped by each grid area code, so we can get the latest delegation for each grid area
        var result = await query
            .OrderByDescending(pd => pd.SequenceNumber)
            .GroupBy(pd => pd.GridAreaCode)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Get the current delegation for each grid area
        var latestForGridAreas = result.Select(group => group.First());

        return latestForGridAreas.ToArray();
    }

    private IQueryable<ProcessDelegation> GetBaseDelegationQuery(
        ActorNumberAndRoleDto? delegatedBy,
        ActorNumberAndRoleDto? delegatedTo,
        ProcessType processType)
    {
        if (delegatedBy == null && delegatedTo == null) throw new ArgumentException("At least one of the delegatedBy or delegatedTo must be set");
        if (delegatedBy != null && delegatedTo != null) throw new ArgumentException("Only one of the delegatedBy or delegatedTo must be set");

        var now = _systemDateTimeProvider.Now();

        // The latest delegation can cover the period from the start date to the end date.
        // If a delegation relationship has been cancelled the EndsAt is set to StartsAt.
        // Therefore, we can not use the EndsAt to determine if the delegation is active in the query.
        var delegationQuery = _masterDataContext.ProcessDelegations
            .Where(pd => pd.DelegatedProcess == processType
                                        && pd.StartsAt <= now
                                        && pd.StopsAt > now);

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
