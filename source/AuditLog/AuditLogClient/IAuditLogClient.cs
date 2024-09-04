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

namespace Energinet.DataHub.EDI.AuditLog.AuditLogServerClient;

/// <summary>
/// Client for submitting audit logs.
/// <remarks>For more information, see the documentation at https://github.com/Energinet-DataHub/opengeh-revision-log/blob/main/docs/documentation-for-submitting-audit-logs.md</remarks>
/// </summary>
public interface IAuditLogClient
{
    /// <summary>
    /// Submit the audit log.
    /// </summary>
    /// <param name="logId">The unique log id, used for idempotency</param>
    /// <param name="userId">Id of the user. If token is available, use the 'sub' claim.</param>
    /// <param name="actorId">Id of the actor. If token is available, use the 'azp' claim.</param>
    /// <param name="systemId">Id of the system that generated the audit. Should be a fixed value for the same subsystem.</param>
    /// <param name="permissions">The set of permissions that were granted when the activity was performed (if any). An example could be the values of the 'role' claims.</param>
    /// <param name="occuredOn">When the activity occured.</param>
    /// <param name="activity">Key of the type of activity that occurred. Examples could be 'CalculationStarted', 'UserCreated' etc.</param>
    /// <param name="origin">Source of the activity within the subsystem. An example could be the full HTTP request URL, including query parameters.</param>
    /// <param name="payload">Payload of the activity, which will be serialized as JSON. An example could be the parsed body af an HTTP request.</param>
    /// <param name="affectedEntityType">Primary type of the entity affected.</param>
    /// <param name="affectedEntityKey">Key (preferably natural key) of the affected entity.</param>
    /// <remarks>
    /// The <paramref name="payload"/> parameter must be a string or an object that can be serialized as JSON.
    /// If the <paramref name="payload"/> is not serializable, then encode it as a base64 string before passing it as an parameter.
    /// </remarks>
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
