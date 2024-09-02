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

using System.Text.Json;
using Energinet.DataHub.EDI.AuditLog.AuditLogClient;
using Energinet.DataHub.EDI.Outbox.Interfaces;

namespace Energinet.DataHub.EDI.AuditLog;

public class AuditLogOutboxMessagePublisher(IAuditLogClient auditLogClient) : IOutboxMessagePublisher
{
    private readonly IAuditLogClient _auditLogClient = auditLogClient;

    public bool CanProcess(string type) => type.Equals(AuditLogOutboxMessageV1.OutboxMessageType, StringComparison.OrdinalIgnoreCase);

    public async Task PublishAsync(string serializedPayload)
    {
        ArgumentException.ThrowIfNullOrEmpty(serializedPayload);

        var payload = JsonSerializer.Deserialize<AuditLogPayload>(serializedPayload)
            ?? throw new InvalidOperationException($"Failed to deserialize payload of type {nameof(AuditLogPayload)}");

        await _auditLogClient.LogAsync(
            logId: payload.LogId,
            userId: payload.UserId,
            actorId: payload.ActorId,
            systemId: payload.SystemId,
            permissions: payload.Permissions,
            occuredOn: payload.OccuredOn,
            activity: payload.Activity,
            origin: payload.Origin,
            payload: payload.Payload,
            affectedEntityType: payload.AffectedEntityType,
            affectedEntityKey: payload.AffectedEntityKey)
                .ConfigureAwait(false);
    }
}
