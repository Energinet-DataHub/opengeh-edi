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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using NodaTime;

namespace Energinet.DataHub.EDI.ArchivedMessages.Domain.Models;

public class ArchivedMeteringPointMessage(
    ArchivedMessageId id,
    string? messageId,
    IReadOnlyList<EventId> eventIds,
    DocumentType documentType,
    ActorNumber senderNumber,
    ActorRole senderRole,
    ActorNumber receiverNumber,
    ActorRole receiverRole,
    Instant createdAt,
    BusinessReason? businessReason,
    ArchivedMessageType archivedMessageType,
    ArchivedMessageStream archivedMessageStream,
    IReadOnlyList<MeteringPointId> meteringPointIds,
    MessageId? relatedToMessageId = null)
    : ArchivedMessage(
        id,
        messageId,
        eventIds,
        documentType,
        senderNumber,
        senderRole,
        receiverNumber,
        receiverRole,
        createdAt,
        businessReason,
        archivedMessageType,
        archivedMessageStream,
        relatedToMessageId)
{
    public IReadOnlyList<MeteringPointId> MeteringPointIds { get; } = meteringPointIds;
}
