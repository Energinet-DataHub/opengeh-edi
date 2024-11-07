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

public class ArchivedMessage
{
    public static readonly FileStorageCategory FileStorageCategory = ArchivedFile.FileStorageCategory;

    /// <summary>
    /// Created an ArchivedMessage from a market document (typically from a outgoing message)
    /// </summary>
    public ArchivedMessage(
        string? messageId,
        IReadOnlyList<EventId> eventIds,
        string documentType,
        ActorNumber senderNumber,
        ActorRole senderRole,
        ActorNumber receiverNumber,
        ActorRole receiverRole,
        Instant createdAt,
        string? businessReason,
        ArchivedMessageType archivedMessageType,
        IMarketDocumentStream marketDocumentStream,
        MessageId? relatedToMessageId = null)
        : this(messageId, eventIds, documentType, senderNumber, senderRole, receiverNumber, receiverRole, createdAt, businessReason, archivedMessageType, new ArchivedMessageStream(marketDocumentStream), relatedToMessageId) { }

    /// <summary>
    /// Creates an ArchivedMessage for an incoming message
    /// </summary>
    public ArchivedMessage(
        string? messageId,
        string documentType,
        ActorNumber senderNumber,
        ActorRole senderRole,
        ActorNumber receiverNumber,
        ActorRole receiverRole,
        Instant createdAt,
        string? businessReason,
        ArchivedMessageType archivedMessageType,
        IIncomingMarketMessageStream incomingMarketMessageStream)
        : this(messageId, Array.Empty<EventId>(), documentType, senderNumber, senderRole, receiverNumber, receiverRole, createdAt, businessReason, archivedMessageType, new ArchivedMessageStream(incomingMarketMessageStream)) { }

    internal ArchivedMessage(
        string? messageId,
        IReadOnlyList<EventId> eventIds,
        string documentType,
        ActorNumber senderNumber,
        ActorRole senderRole,
        ActorNumber receiverNumber,
        ActorRole receiverRole,
        Instant createdAt,
        string? businessReason,
        ArchivedMessageType archivedMessageType,
        ArchivedMessageStream archivedMessageStream,
        MessageId? relatedToMessageId = null)
    {
        Id = ArchivedMessageId.Create();
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

    public ArchivedMessageId Id { get; }

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

    public ArchivedMessageStream ArchivedMessageStream { get; }

    private static string GetActorNumberForFileStoragePlacement(ArchivedMessageType archivedMessageType, string senderActorNumber, string receiverActorNumber)
    {
        return archivedMessageType switch
        {
            ArchivedMessageType.IncomingMessage => senderActorNumber,
            ArchivedMessageType.OutgoingMessage => receiverActorNumber,
            _ => throw new ArgumentOutOfRangeException(nameof(archivedMessageType), archivedMessageType, "Unknown ArchivedMessageType"),
        };
    }
}
