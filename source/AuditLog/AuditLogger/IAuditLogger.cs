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

namespace Energinet.DataHub.EDI.AuditLog.AuditLogger;

/// <summary>
/// Audit logger for logging audit logs according to the documentation found in:
/// https://github.com/Energinet-DataHub/opengeh-revision-log/blob/main/docs/documentation-for-submitting-audit-logs.md
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Log an audit log entry asynchronously.
    /// </summary>
    /// <param name="logId">Globally unique audit log id, used for idempotency.</param>
    /// <param name="activity">The performed activity</param>
    /// <param name="activityOrigin">
    /// Source of the activity. An example in case of a HTTP request could be the route (including query parameters)
    /// that triggered the activity
    /// </param>
    /// <param name="activityPayload">
    /// Additional information about the activity. An example in case of a HTTP request could be the request POST body.
    /// If the payload is not a string, it will be converted to a base64 encoded string.
    /// </param>
    /// <param name="affectedEntityType">Primary type of the entity affected. If multiple entities are effected, consider whether it warrants an audit log per entity or not.</param>
    /// <param name="affectedEntityKey">Key (preferably natural key) of the affected entity.</param>
    /// <param name="cancellationToken"></param>
    Task LogAsync(
        AuditLogId logId,
        AuditLogActivity activity,
        string activityOrigin,
        object? activityPayload,
        AuditLogEntityType? affectedEntityType,
        string? affectedEntityKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Log an audit log entry and commit the transaction immediately.
    /// </summary>
    /// <param name="logId">Globally unique audit log id, used for idempotency.</param>
    /// <param name="activity">The performed activity</param>
    /// <param name="activityOrigin">
    /// Source of the activity. An example in case of a HTTP request could be the route (including query parameters)
    /// that triggered the activity
    /// </param>
    /// <param name="activityPayload">
    /// Additional information about the activity. An example in case of a HTTP request could be the request POST body.
    /// If the payload is not a string, it will be converted to a base64 encoded string.
    /// </param>
    /// <param name="affectedEntityType">Primary type of the entity affected. If multiple entities are effected, consider whether it warrants an audit log per entity or not.</param>
    /// <param name="affectedEntityKey">Key (preferably natural key) of the affected entity.</param>
    /// <param name="cancellationToken"></param>
    Task LogWithCommitAsync(
        AuditLogId logId,
        AuditLogActivity activity,
        string activityOrigin,
        object? activityPayload,
        AuditLogEntityType? affectedEntityType,
        string? affectedEntityKey,
        CancellationToken cancellationToken = default);
}
