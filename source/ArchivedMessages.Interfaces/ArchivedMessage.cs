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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using NodaTime;

namespace Energinet.DataHub.EDI.ArchivedMessages.Interfaces;

public class ArchivedMessage
{
    public static readonly FileStorageCategory FileStorageCategory = ArchivedFile.FileStorageCategory;

    public ArchivedMessage(
        string? messageId,
        string documentType,
        string senderNumber, // Doesn't use ActorNumber since we want to make sure to always create a ArchivedMessage
        string receiverNumber, // Doesn't use ActorNumber since we want to make sure to always create a ArchivedMessage
        Instant createdAt,
        string? businessReason,
        ArchivedMessageType archivedMessageType,
        Stream document)
    {
        Id = ArchivedMessageId.Create();
        MessageId = messageId;
        DocumentType = documentType;
        SenderNumber = senderNumber;
        ReceiverNumber = receiverNumber;
        CreatedAt = createdAt;
        BusinessReason = businessReason;
        Document = document;

        var actorNumberForFileStorage = GetActorNumberForFileStoragePlacement(archivedMessageType, senderNumber, receiverNumber);
        FileStorageReference = FileStorageReference.Create(FileStorageCategory, actorNumberForFileStorage, createdAt, Id.Value);
    }

    public ArchivedMessageId Id { get; }

    public string? MessageId { get; }

    public string DocumentType { get; }

    public string SenderNumber { get; }

    public string ReceiverNumber { get; }

    public Instant CreatedAt { get; }

    public string? BusinessReason { get; }

    public FileStorageReference FileStorageReference { get; }

    public Stream Document { get; }

    private string GetActorNumberForFileStoragePlacement(ArchivedMessageType archivedMessageType, string senderActorNumber, string receiverActorNumber)
    {
        return archivedMessageType switch
        {
            ArchivedMessageType.IncomingMessage => SenderNumber,
            ArchivedMessageType.OutgoingMessage => ReceiverNumber,
            _ => throw new ArgumentOutOfRangeException(nameof(archivedMessageType), archivedMessageType, "Unknown ArchivedMessageType"),
        };
    }
}
