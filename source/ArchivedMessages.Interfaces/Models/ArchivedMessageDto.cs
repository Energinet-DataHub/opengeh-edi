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
        string documentType,
        ActorNumber senderNumber,
        ActorRole senderRole,
        ActorNumber receiverNumber,
        ActorRole receiverRole,
        Instant createdAt,
        string? businessReason,
        ArchivedMessageTypeDto archivedMessageType,
        IMarketDocumentStream marketDocumentStream,
        MessageId? relatedToMessageId = null)
        : this(messageId, eventIds, documentType, senderNumber, senderRole, receiverNumber, receiverRole, createdAt, businessReason, archivedMessageType, new ArchivedMessageStreamDto(marketDocumentStream), relatedToMessageId) { }

    public ArchivedMessageDto(
        string? messageId,
        string documentType,
        ActorNumber senderNumber,
        ActorRole senderRole,
        ActorNumber receiverNumber,
        ActorRole receiverRole,
        Instant createdAt,
        string? businessReason,
        ArchivedMessageTypeDto archivedMessageType,
        IIncomingMarketMessageStream incomingMarketMessageStream,
        MessageId? relatedToMessageId = null)
        : this(messageId, Array.Empty<EventId>(), documentType, senderNumber, senderRole, receiverNumber, receiverRole, createdAt, businessReason, archivedMessageType, new ArchivedMessageStreamDto(incomingMarketMessageStream)) { }

    public ArchivedMessageDto(
        string? messageId,
        IReadOnlyList<EventId> eventIds,
        string documentType,
        ActorNumber senderNumber,
        ActorRole senderRole,
        ActorNumber receiverNumber,
        ActorRole receiverRole,
        Instant createdAt,
        string? businessReason,
        ArchivedMessageTypeDto archivedMessageType,
        ArchivedMessageStreamDto archivedMessageStream,
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

        var actorNumberForFileStorage = GetActorNumberForFileStoragePlacement(
            archivedMessageType,
            senderNumber.Value,
            receiverNumber.Value);
        FileStorageReference = FileStorageReference.Create(
            FileStorageCategory,
            actorNumberForFileStorage,
            createdAt,
            Id.Value);
    }

    public ArchivedMessageIdDto Id { get; }

    public string? MessageId { get; }

    public IReadOnlyList<EventId> EventIds { get; }

    public string DocumentType { get; }

    public ActorNumber SenderNumber { get; }

    public ActorRole SenderRole { get; }

    public ActorNumber ReceiverNumber { get; }

    public ActorRole ReceiverRole { get; }

    public Instant CreatedAt { get; }

    public string? BusinessReason { get; }

    public MessageId? RelatedToMessageId { get; private set; }

    public FileStorageReference FileStorageReference { get; }

    public ArchivedMessageTypeDto ArchivedMessageType { get; }

    public ArchivedMessageStreamDto ArchivedMessageStream { get; }

    private static string GetActorNumberForFileStoragePlacement(ArchivedMessageTypeDto archivedMessageType, string senderActorNumber, string receiverActorNumber)
    {
        return archivedMessageType switch
        {
            ArchivedMessageTypeDto.IncomingMessage => senderActorNumber,
            ArchivedMessageTypeDto.OutgoingMessage => receiverActorNumber,
            _ => throw new ArgumentOutOfRangeException(nameof(archivedMessageType), archivedMessageType, "Unknown ArchivedMessageType"),
        };
    }
}
