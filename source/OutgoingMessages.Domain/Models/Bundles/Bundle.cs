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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using NodaTime;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
// Consider declaring as nullable.

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;

public sealed class Bundle
{
    private readonly int _maxNumberOfMessagesInABundle;
    private int _messageCount;

    internal Bundle(
        BusinessReason businessReason,
        DocumentType documentTypeInBundle,
        int maxNumberOfMessagesInABundle,
        Instant created,
        MessageId? relatedToMessageId)
    {
        _maxNumberOfMessagesInABundle = maxNumberOfMessagesInABundle;
        Id = BundleId.New();
        MessageId = GloriousEbixUuid.FromGuid(Id.Id);
        BusinessReason = businessReason;
        DocumentTypeInBundle = documentTypeInBundle;
        Created = created;
        RelatedToMessageId = relatedToMessageId;
    }

    private Bundle()
    {
    }

    public bool IsDequeued { get; private set; }

    public Instant Created { get; private set; }

    /// <summary>
    /// If this attribute has a value, then it is used to store the message id of a request from an actor.
    /// Giving us the possibility to track the request and the response.
    /// </summary>
    public MessageId? RelatedToMessageId { get; private set; }

    public GloriousEbixUuid MessageId { get; private set; }

    internal DocumentType DocumentTypeInBundle { get; }

    internal BundleId Id { get; }

    internal BusinessReason BusinessReason { get; }

    internal bool IsClosed { get; private set; }

    public void CloseBundle()
    {
        IsClosed = true;
    }

    internal void Add(OutgoingMessage outgoingMessage)
    {
        if (IsClosed)
            return;

        outgoingMessage.AssignToBundle(Id);
        _messageCount++;
        CloseBundleIfFull();
    }

    internal void Dequeue()
    {
        IsDequeued = true;
    }

    private void CloseBundleIfFull()
    {
        IsClosed = _maxNumberOfMessagesInABundle == _messageCount;
    }
}
