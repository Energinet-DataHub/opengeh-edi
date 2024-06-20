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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;

/// <summary>
/// Base contract for an outgoing message
/// </summary>
public abstract class OutgoingMessageDto
{
    protected OutgoingMessageDto(
        DocumentType documentType,
        ActorNumber receiverNumber,
        Guid? processId,
        EventId eventId,
        string businessReasonName,
        ActorRole receiverRole,
        ActorNumber senderId,
        ActorRole senderRole,
        MessageId? relatedToMessageId = null)
    {
        DocumentType = documentType;
        ReceiverNumber = receiverNumber;
        ProcessId = processId;
        EventId = eventId;
        BusinessReason = businessReasonName;
        ReceiverRole = receiverRole;
        SenderId = senderId;
        SenderRole = senderRole;
        RelatedToMessageId = relatedToMessageId;
    }

    public DocumentType DocumentType { get; }

    public ActorNumber ReceiverNumber { get; }

    public Guid? ProcessId { get; }

    /// <summary>
    /// Stores the id of the service bus message that created the OutgoingMessage
    ///     Useful for tracking and debugging purposes.
    /// </summary>
    public EventId EventId { get; }

    public string BusinessReason { get; }

    public ActorRole ReceiverRole { get; }

    public ActorNumber SenderId { get; }

    public ActorRole SenderRole { get; }

    /// <summary>
    /// If this attribute has a value, then it is used to store the message id of a request from an actor.
    /// Giving us the possibility to track the request and the response.
    /// </summary>
    public MessageId? RelatedToMessageId { get; }
}
