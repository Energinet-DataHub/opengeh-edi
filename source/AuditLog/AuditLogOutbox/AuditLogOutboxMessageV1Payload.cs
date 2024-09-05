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

namespace Energinet.DataHub.EDI.AuditLog.AuditLogOutbox;

/// <summary>
/// The serialized payload of an audit log outbox message. Changes must be backward compatible, or a new version
/// must be introduced.
/// </summary>
public record AuditLogOutboxMessageV1Payload(
    Guid LogId,
    Guid UserId,
    Guid ActorId,
    Guid SystemId,
    string? Permissions,
    Instant OccuredOn,
    string Activity,
    string Origin,
    object? Payload,
    string? AffectedEntityType,
    string? AffectedEntityKey);
