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

using Energinet.DataHub.EDI.AuditLog.AuditLogOutbox;
using Energinet.DataHub.EDI.AuditLog.AuditUser;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.Outbox.Interfaces;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace Energinet.DataHub.EDI.AuditLog.AuditLogger;

public class AuditLogger(
    IClock clock,
    ISerializer serializer,
    IAuditUserContext auditUserContext,
    IOutboxClient outbox,
    IUnitOfWork unitOfWork) : IAuditLogger
{
    private static readonly Guid _ediSystemId = Guid.Parse("688b2dca-7231-490f-a731-d7869d33fe5e");

    private readonly IClock _clock = clock;
    private readonly ISerializer _serializer = serializer;
    private readonly IAuditUserContext _auditUserContext = auditUserContext;
    private readonly IOutboxClient _outbox = outbox;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task LogAsync(
        AuditLogId logId,
        AuditLogActivity activity,
        string activityOrigin,
        object? activityPayload,
        AuditLogEntityType? affectedEntityType,
        string? affectedEntityKey)
    {
        var currentUser = _auditUserContext.CurrentUser;

        var userId = currentUser?.UserId ?? Guid.Empty;
        var actorId = currentUser?.ActorId ?? Guid.Empty;
        var permissions = currentUser?.Permissions;

        var outboxMessage = new AuditLogOutboxMessageV1(
            _serializer,
            new AuditLogOutboxMessageV1Payload(
                logId.Id,
                userId,
                actorId,
                _ediSystemId,
                permissions,
                _clock.GetCurrentInstant(),
                activity.Identifier,
                activityOrigin,
                activityPayload,
                affectedEntityType?.Identifier,
                affectedEntityKey));

        await _outbox.CreateWithoutCommitAsync(outboxMessage)
            .ConfigureAwait(false);
    }

    public async Task LogWithCommitAsync(
        AuditLogId logId,
        AuditLogActivity activity,
        string activityOrigin,
        object? activityPayload,
        AuditLogEntityType? affectedEntityType,
        string? affectedEntityKey)
    {
        await LogAsync(
            logId,
            activity,
            activityOrigin,
            activityPayload,
            affectedEntityType,
            affectedEntityKey)
            .ConfigureAwait(false);

        await _unitOfWork.CommitTransactionAsync()
            .ConfigureAwait(false);
    }
}
