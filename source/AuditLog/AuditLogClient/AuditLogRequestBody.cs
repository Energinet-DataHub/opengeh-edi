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

namespace Energinet.DataHub.EDI.AuditLog.AuditLogClient;

/// <summary>
/// The HTTP request body for submitting an audit log entry to the shared audit log server.
/// <remarks>
/// The shared audit log server documentation is available at
/// https://github.com/Energinet-DataHub/opengeh-revision-log/blob/main/docs/documentation-for-submitting-audit-logs.md
/// </remarks>
/// </summary>
[Serializable]
public record AuditLogRequestBody(
    Guid LogId,
    Guid UserId,
    Guid ActorId,
    Guid SystemId,
    string? Permissions,
    string OccurredOn,
    string Activity,
    string Origin,
    string Payload,
    string AffectedEntityType,
    string AffectedEntityKey);
