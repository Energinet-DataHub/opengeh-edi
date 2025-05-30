﻿// Copyright 2020 Energinet DataHub A/S
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

public class ArchivedMessage
{
    public static readonly FileStorageCategory FileStorageCategory = ArchivedFile.FileStorageCategory;

    public ArchivedMessage(
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
        MessageId? relatedToMessageId = null)
    {
        Id = id;
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

    public DocumentType DocumentType { get; }

    public ActorNumber SenderNumber { get; }

    public ActorRole SenderRole { get; }

    public ActorNumber ReceiverNumber { get; }

    public ActorRole ReceiverRole { get; }

    public Instant CreatedAt { get; }

    public BusinessReason? BusinessReason { get; }

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
