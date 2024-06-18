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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using NodaTime;

namespace Energinet.DataHub.EDI.ArchivedMessages.Interfaces;

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
        string senderNumber, // Doesn't use ActorNumber since we want to make sure to always create a ArchivedMessage
        string receiverNumber, // Doesn't use ActorNumber since we want to make sure to always create a ArchivedMessage
        Instant createdAt,
        string? businessReason,
        ArchivedMessageType archivedMessageType,
        IMarketDocumentStream marketDocumentStream,
        MessageId? relatedToMessageId = null)
        : this(messageId, eventIds, documentType, senderNumber, receiverNumber, createdAt, businessReason, archivedMessageType, new ArchivedMessageStream(marketDocumentStream), relatedToMessageId) { }

    /// <summary>
    /// Creates an ArchivedMessage for an incoming message
    /// </summary>
    public ArchivedMessage(
        string? messageId,
        string documentType,
        string senderNumber, // Doesn't use ActorNumber since we want to make sure to always create a ArchivedMessage
        string receiverNumber, // Doesn't use ActorNumber since we want to make sure to always create a ArchivedMessage
        Instant createdAt,
        string? businessReason,
        ArchivedMessageType archivedMessageType,
        IIncomingMarketMessageStream incomingMarketMessageStream)
        : this(messageId, Array.Empty<EventId>(), documentType, senderNumber, receiverNumber, createdAt, businessReason, archivedMessageType, new ArchivedMessageStream(incomingMarketMessageStream)) { }

    private ArchivedMessage(
        string? messageId,
        IReadOnlyList<EventId> eventIds,
        string documentType,
        string senderNumber, // Doesn't use ActorNumber since we want to make sure to always create a ArchivedMessage
        string receiverNumber, // Doesn't use ActorNumber since we want to make sure to always create a ArchivedMessage
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
        ReceiverNumber = receiverNumber;
        CreatedAt = createdAt;
        BusinessReason = businessReason;
        RelatedToMessageId = relatedToMessageId;
        ArchivedMessageStream = archivedMessageStream;

        var actorNumberForFileStorage = GetActorNumberForFileStoragePlacement(archivedMessageType, senderNumber, receiverNumber);
        FileStorageReference = FileStorageReference.Create(FileStorageCategory, actorNumberForFileStorage, createdAt, Id.Value);
    }

    public ArchivedMessageId Id { get; }

    public string? MessageId { get; }

    public IReadOnlyList<EventId> EventIds { get; }

    public string DocumentType { get; }

    public string SenderNumber { get; }

    public string ReceiverNumber { get; }

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
