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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using NodaTime;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
// Consider declaring as nullable.

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;

public sealed class Bundle
{
    private readonly int _maxNumberOfMessagesInABundle;
    private int _messageCount;

    /// <summary>
    /// Create new bundle in the given actor message queue
    /// </summary>
    public Bundle(
        ActorMessageQueueId actorMessageQueueId,
        BusinessReason businessReason,
        DocumentType documentTypeInBundle,
        int maxNumberOfMessagesInABundle,
        Instant created,
        MessageId? relatedToMessageId)
    {
        _maxNumberOfMessagesInABundle = maxNumberOfMessagesInABundle;
        Id = BundleId.New();
        ActorMessageQueueId = actorMessageQueueId;
        MessageId = MessageId.New();
        BusinessReason = businessReason;
        DocumentTypeInBundle = documentTypeInBundle;
        Created = created;
        RelatedToMessageId = relatedToMessageId;
        MessageCategory = DocumentTypeInBundle.Category;
    }

    private Bundle()
    {
    }

    public BundleId Id { get; }

    public ActorMessageQueueId ActorMessageQueueId { get; }

    public Instant? DequeuedAt { get; private set; }

    public Instant? PeekedAt { get; private set; }

    public Instant Created { get; private set; }

    /// <summary>
    /// If this attribute has a value, then it is used to store the message id of a request from an actor.
    /// Giving us the possibility to track the request and the response.
    /// </summary>
    public MessageId? RelatedToMessageId { get; private set; }

    public MessageId MessageId { get; private set; }

    public DocumentType DocumentTypeInBundle { get; }

    public BusinessReason BusinessReason { get; }

    public Instant? ClosedAt { get; private set; }

    public MessageCategory MessageCategory { get; set; }

    public void PeekBundle()
    {
        // If the bundle is closed, because it was full. Then we should not update it at the peeked time.
        ClosedAt ??= SystemClock.Instance.GetCurrentInstant();
        PeekedAt = SystemClock.Instance.GetCurrentInstant();
    }

    public void Add(OutgoingMessage outgoingMessage)
    {
        if (ClosedAt is not null)
            throw new InvalidOperationException($"Cannot add message to a closed bundle (bundle id: {Id.Id}, message id: {outgoingMessage.Id}, external id: {outgoingMessage.ExternalId})");

        outgoingMessage.AssignToBundle(Id);
        _messageCount++;
        CloseBundleIfFull(outgoingMessage.CreatedAt);
    }

    public bool TryDequeue()
    {
        if (ClosedAt is not null && PeekedAt is not null)
        {
            DequeuedAt = SystemClock.Instance.GetCurrentInstant();
            return true;
        }

        return false;
    }

    public void Close()
    {
        ClosedAt = SystemClock.Instance.GetCurrentInstant();
    }

    private void CloseBundleIfFull(Instant messageCreatedAt)
    {
        if (_maxNumberOfMessagesInABundle == _messageCount)
           ClosedAt = messageCreatedAt;
    }
}
