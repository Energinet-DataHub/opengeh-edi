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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.MasterData.Domain.ProcessDelegations;
using Energinet.DataHub.EDI.MasterData.Infrastructure.DataAccess;
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

    public async Task<ProcessDelegation?> GetAsync(
        ActorNumber delegatedByActorNumber,
        ActorRole delegatedByActorRole,
        string? gridAreaCode,
        ProcessType processType,
        CancellationToken cancellationToken)
    {
        var now = _systemDateTimeProvider.Now();

        // The latest delegation can cover the period from the start date to the end date.
        // If a delegation relationship has been cancelled the EndsAt is set to StartsAt.
        // Therefore, we can not use the EndsAt to determine if the delegation is active in the query.
        var delegationQuery = _masterDataContext.ProcessDelegations
            .Where(
                processDelegation => processDelegation.DelegatedByActorNumber == delegatedByActorNumber
                                     && processDelegation.DelegatedByActorRole == delegatedByActorRole
                                     && processDelegation.DelegatedProcess == processType
                                     && processDelegation.StartsAt <= now);

        if (gridAreaCode != null)
        {
            delegationQuery = delegationQuery.Where(processDelegation => processDelegation.GridAreaCode == gridAreaCode);
        }

        var delegation = await delegationQuery
            .OrderByDescending(processDelegation => processDelegation.SequenceNumber)
            .ThenByDescending(processDelegation => processDelegation.StartsAt)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        if (delegation == null)
            return null;

        // If StopsAt is less than or equal to the current time, the delegation has ended.
        if (delegation.StopsAt <= now)
            return null;

        return delegation;
    }
}
