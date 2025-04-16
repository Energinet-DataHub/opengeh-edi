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

namespace Energinet.DataHub.EDI.ArchivedMessages.Interfaces.Models;

public class ArchivedMessageDto
{
    public static readonly FileStorageCategory FileStorageCategory = ArchivedFile.FileStorageCategory;

    public ArchivedMessageDto(
        string? messageId,
        IReadOnlyList<EventId> eventIds,
        DocumentType documentType,
        ActorNumber senderNumber,
        ActorRole senderRole,
        ActorNumber receiverNumber,
        ActorRole receiverRole,
        Instant createdAt,
        BusinessReason? businessReason,
        ArchivedMessageTypeDto archivedMessageType,
        IMarketDocumentStream marketDocumentStream,
        IReadOnlyList<MeteringPointId> meteringPointIds,
        MessageId? relatedToMessageId = null)
        : this(messageId, eventIds, documentType, senderNumber, senderRole, receiverNumber, receiverRole, createdAt, businessReason, archivedMessageType, new ArchivedMessageStreamDto(marketDocumentStream), meteringPointIds, relatedToMessageId) { }

    public ArchivedMessageDto(
        string? messageId,
        DocumentType documentType,
        ActorNumber senderNumber,
        ActorRole senderRole,
        ActorNumber receiverNumber,
        ActorRole receiverRole,
        Instant createdAt,
        BusinessReason? businessReason,
        ArchivedMessageTypeDto archivedMessageType,
        IIncomingMarketMessageStream incomingMarketMessageStream,
        IReadOnlyList<MeteringPointId> meteringPointIds)
        : this(messageId, Array.Empty<EventId>(), documentType, senderNumber, senderRole, receiverNumber, receiverRole, createdAt, businessReason, archivedMessageType, new ArchivedMessageStreamDto(incomingMarketMessageStream), meteringPointIds) { }

    public ArchivedMessageDto(
        string? messageId,
        IReadOnlyList<EventId> eventIds,
        DocumentType documentType,
        ActorNumber senderNumber,
        ActorRole senderRole,
        ActorNumber receiverNumber,
        ActorRole receiverRole,
        Instant createdAt,
        BusinessReason? businessReason,
        ArchivedMessageTypeDto archivedMessageType,
        ArchivedMessageStreamDto archivedMessageStream,
        IReadOnlyList<MeteringPointId> meteringPointIds,
        MessageId? relatedToMessageId = null)
    {
        Id = ArchivedMessageIdDto.Create();
        MessageId = messageId;
        EventIds = eventIds;
        DocumentType = documentType;
        SenderNumber = senderNumber;
        SenderRole = senderRole;
        ReceiverNumber = receiverNumber;
        ReceiverRole = receiverRole;
        CreatedAt = createdAt;
        BusinessReason = businessReason;
        RelatedToMessageId = relatedToMessageId;
        ArchivedMessageType = archivedMessageType;
        ArchivedMessageStream = archivedMessageStream;
        MeteringPointIds = meteringPointIds;
    }

    public ArchivedMessageIdDto Id { get; }

    public string? MessageId { get; }

    public IReadOnlyList<EventId> EventIds { get; }

    public DocumentType DocumentType { get; }

    public ActorNumber SenderNumber { get; }

    public ActorRole SenderRole { get; }

    public ActorNumber ReceiverNumber { get; }

    public ActorRole ReceiverRole { get; }

    public Instant CreatedAt { get; }

    public BusinessReason? BusinessReason { get; }

    public MessageId? RelatedToMessageId { get; private set; }

    public ArchivedMessageTypeDto ArchivedMessageType { get; }

    public ArchivedMessageStreamDto ArchivedMessageStream { get; }

    public IReadOnlyList<MeteringPointId> MeteringPointIds { get; set; }
}
