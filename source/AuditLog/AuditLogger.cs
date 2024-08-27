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

using Energinet.DataHub.EDI.AuditLog.AuditLogClient;
using Energinet.DataHub.EDI.AuditLog.AuditUser;
using NodaTime;

namespace Energinet.DataHub.EDI.AuditLog;

public class AuditLogger(IClock clock, IAuditUserContext auditUserContext, IAuditLogClient auditLogClient) : IAuditLogger
{
    private static readonly Guid _ediSystemId = new("edi06cd7-61ae-4943-a684-ac2f2681a3b1");

    private readonly IClock _clock = clock;
    private readonly IAuditUserContext _auditUserContext = auditUserContext;
    private readonly IAuditLogClient _auditLogClient = auditLogClient;

    public Task LogAsync(
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

        return _auditLogClient.LogAsync(
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
            affectedEntityKey);
    }
}
