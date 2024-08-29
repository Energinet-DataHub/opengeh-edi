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

using NodaTime;

namespace Energinet.DataHub.EDI.AuditLog.AuditLogClient;

/// <summary>
/// Client for saving audit logs. Documentation for the audit logs can be found at:
/// https://github.com/Energinet-DataHub/opengeh-revision-log/blob/main/docs/documentation-for-submitting-audit-logs.md
/// </summary>
public interface IAuditLogClient
{
    /// <summary>
    /// Persist the audit log
    /// </summary>
    Task LogAsync(
        Guid logId,
        Guid userId,
        Guid actorId,
        Guid systemId,
        string? permissions,
        Instant occuredOn,
        string activity,
        string origin,
        object? payload,
        string? affectedEntityType,
        string? affectedEntityKey);
}
