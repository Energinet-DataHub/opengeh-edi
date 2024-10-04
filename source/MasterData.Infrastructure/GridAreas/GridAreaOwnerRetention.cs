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

using Energinet.DataHub.EDI.AuditLog.AuditLogger;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.TimeEvents;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.MasterData.Infrastructure.DataAccess;
using NodaTime;

namespace Energinet.DataHub.EDI.MasterData.Infrastructure.GridAreas;

public class GridAreaOwnerRetention : IDataRetention
{
    private readonly IClock _clock;
    private readonly MasterDataContext _masterDataContext;
    private readonly IAuditLogger _auditLogger;

    public GridAreaOwnerRetention(
        IClock clock,
        MasterDataContext masterDataContext,
        IAuditLogger auditLogger)
    {
        _clock = clock;
        _masterDataContext = masterDataContext;
        _auditLogger = auditLogger;
    }

    public async Task CleanupAsync(CancellationToken cancellationToken)
    {
        var now = _clock.GetCurrentInstant();
        var monthAgo = now.Plus(-Duration.FromDays(30));
        _masterDataContext.GridAreaOwners.RemoveRange(
            _masterDataContext.GridAreaOwners
                 .Where(x => x.ValidFrom < monthAgo)
                 .Where(x => _masterDataContext.GridAreaOwners.Any(y =>
                     y.GridAreaCode == x.GridAreaCode
                     && y.ValidFrom < now
                     && y.SequenceNumber > x.SequenceNumber)));

        await _auditLogger.LogWithCommitAsync(
                logId: AuditLogId.New(),
                activity: AuditLogActivity.RetentionDeletion,
                activityOrigin: nameof(ADayHasPassed),
                activityPayload: monthAgo,
                affectedEntityType: AuditLogEntityType.GridAreaOwner,
                affectedEntityKey: null)
            .ConfigureAwait(false);

        await _masterDataContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
