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

using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces.Models;

namespace Energinet.DataHub.EDI.MasterData.Application;

public class AuditLogger(IAuditUserContext auditUserContext) : IAuditLogger
{
    private readonly IAuditUserContext _auditUserContext = auditUserContext;

    public Task LogAsync(
        AuditLogId id,
        AuditLogActivity activity,
        string activityOrigin,
        object? activityPayload,
        AuditLogEntityType affectedEntityType,
        string affectedEntityKey)
    {
        var currentUser = _auditUserContext.CurrentUser;

        var userId = currentUser?.UserId ?? Guid.Empty;
        var actorId = currentUser?.ActorId ?? Guid.Empty;
        var permissions = currentUser?.Permissions;

        Console.WriteLine("Audit log");
        return Task.CompletedTask;
    }
}
