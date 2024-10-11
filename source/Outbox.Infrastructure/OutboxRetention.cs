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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Text;

namespace Energinet.DataHub.EDI.Outbox.Infrastructure;

public class OutboxRetention(
    OutboxContext outboxContext,
    IClock clock,
    ILogger<OutboxRetention> logger,
    IAuditLogger auditLogger) : IDataRetention
{
    private readonly OutboxContext _outboxContext = outboxContext;
    private readonly IClock _clock = clock;
    private readonly ILogger<OutboxRetention> _logger = logger;
    private readonly IAuditLogger _auditLogger = auditLogger;

    /// <summary>
    /// Deletes all messages that have been processed more than a week ago.
    /// </summary>
    public async Task CleanupAsync(CancellationToken cancellationToken)
    {
        var oneWeekAgo = _clock.GetCurrentInstant().Minus(Duration.FromDays(7));

        var batchSize = 100;
        var skip = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var messagesToDelete = await _outboxContext.Outbox
                    .Where(om => om.PublishedAt != null && om.PublishedAt < oneWeekAgo)
                    .Skip(skip)
                    .Take(batchSize)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (messagesToDelete.Count == 0)
                    break;

                _outboxContext.Outbox.RemoveRange(messagesToDelete);

                // TODO: use cancellation token
                await _auditLogger.LogWithCommitAsync(
                        logId: AuditLogId.New(),
                        activity: AuditLogActivity.RetentionDeletion,
                        activityOrigin: nameof(ADayHasPassed),
                        activityPayload: (OlderThan: oneWeekAgo, DeletedAmount: messagesToDelete.Count),
                        affectedEntityType: AuditLogEntityType.OutboxMessage,
                        affectedEntityKey: null,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                await _outboxContext.SaveChangesAsync(cancellationToken)
                    .ConfigureAwait(false);

                skip += messagesToDelete.Count;
            }
            catch (Exception e)
            {
                _logger.LogError(
                    e,
                    "An error occurred while deleting outbox messages older than a {DeleteMessagesOlderThan}. Deleted {DeletedMessagesCount} messages.",
                    InstantPattern.General.Format(oneWeekAgo),
                    skip);
            }
        }
    }
}
