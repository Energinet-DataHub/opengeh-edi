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

using Energinet.DataHub.Core.Outbox.Abstractions;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.RevisionLog.Integration;

namespace Energinet.DataHub.EDI.AuditLog.AuditLogOutbox;

public class AuditLogOutboxPublisher(IRevisionLogClient revisionLogClient, ISerializer serializer) : IOutboxPublisher
{
    private readonly IRevisionLogClient _revisionLogClient = revisionLogClient;
    private readonly ISerializer _serializer = serializer;

    public bool CanPublish(string type) => type.Equals(AuditLogOutboxMessageV1.OutboxMessageType, StringComparison.OrdinalIgnoreCase);

    public async Task PublishAsync(string serializedPayload)
    {
        ArgumentException.ThrowIfNullOrEmpty(serializedPayload);

        var payload = _serializer.Deserialize<AuditLogOutboxMessageV1Payload>(serializedPayload)
            ?? throw new InvalidOperationException($"Failed to deserialize payload of type {nameof(AuditLogOutboxMessageV1Payload)}");

        var payloadAsJson = payload.Payload switch
        {
            null => string.Empty,
            string p => p,
            _ => _serializer.Serialize(payload),
        };

        await _revisionLogClient.LogAsync(new RevisionLogEntry(
            logId: payload!.LogId,
            userId: payload.UserId,
            actorId: payload.ActorId,
            actorNumber: payload.ActorNumber,
            marketRoles: payload.MarketRoles,
            systemId: payload.SystemId,
            permissions: payload.Permissions,
            occurredOn: payload.OccuredOn,
            activity: payload.Activity,
            origin: payload.Origin,
            payload: payloadAsJson,
            affectedEntityType: payload.AffectedEntityType,
            affectedEntityKey: payload.AffectedEntityKey))
                .ConfigureAwait(false);
    }
}
