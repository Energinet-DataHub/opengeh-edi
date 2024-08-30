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

namespace Energinet.DataHub.EDI.AuditLog.AuditUser;

/// <summary>
/// The user required for submitting audit logs.
/// </summary>
/// <param name="UserId">The user id, typically found in a 'sub' claim.</param>
/// <param name="ActorId">The user id, typically found in an 'azp' claim.</param>
/// <param name="Permissions">The user's permission, typically found in 'role' claims</param>
public record AuditUser(
    Guid? UserId,
    Guid? ActorId,
    string? Permissions);
