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

using System.Diagnostics.CodeAnalysis;
using System.IO;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using NodaTime;

namespace Energinet.DataHub.EDI.ArchivedMessages.Interfaces;

public class ArchivedMessage
{
    public const string FileStorageCategory = "archived";

    public ArchivedMessage(
        string id,
        string? messageId,
        string documentType,
        ActorNumber senderNumber,
        ActorNumber receiverNumber,
        Instant createdAt,
        string? businessReason,
        Stream document)
    {
        Id = id;
        MessageId = messageId;
        DocumentType = documentType;
        SenderNumber = senderNumber;
        ReceiverNumber = receiverNumber;
        CreatedAt = createdAt;
        BusinessReason = businessReason;
        Document = document;

        FileStorageReference = FileStorageReference.Create(FileStorageCategory, ReceiverNumber, createdAt, Id);
    }

    public string Id { get; }

    public string? MessageId { get; }

    public string DocumentType { get; }

    public ActorNumber SenderNumber { get; }

    public ActorNumber ReceiverNumber { get; }

    public Instant CreatedAt { get; }

    public string? BusinessReason { get; }

    public FileStorageReference FileStorageReference { get; }

    public Stream Document { get; }
}
