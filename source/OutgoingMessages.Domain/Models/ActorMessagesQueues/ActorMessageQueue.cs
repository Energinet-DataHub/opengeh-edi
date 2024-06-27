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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;

#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class ActorMessageQueue
{
    // Used for persistent actor message queue entity.
    private readonly Guid _id;
    private readonly List<Bundle> _bundles = new();

    private ActorMessageQueue(Receiver receiver)
    {
        Receiver = receiver;
        _id = Guid.NewGuid();
    }

    public Receiver Receiver { get; set; }

    #pragma warning disable
    private ActorMessageQueue()
    {
    }

    public static ActorMessageQueue CreateFor(Receiver receiver)
    {
        return new ActorMessageQueue(receiver);
    }

    public void Enqueue(OutgoingMessage outgoingMessage, Instant timeStamp, int? maxNumberOfMessagesInABundle = null)
    {
        ArgumentNullException.ThrowIfNull(outgoingMessage);
        EnsureApplicable(outgoingMessage);

        var currentBundle = CurrentBundleOf(BusinessReason.FromName(outgoingMessage.BusinessReason), outgoingMessage.DocumentType, outgoingMessage.RelatedToMessageId) ??
                            CreateBundleOf(BusinessReason.FromName(outgoingMessage.BusinessReason), outgoingMessage.DocumentType, GetMaxNumberOfMessagesInABundle(maxNumberOfMessagesInABundle, outgoingMessage.DocumentType), timeStamp, outgoingMessage.RelatedToMessageId);

        currentBundle.Add(outgoingMessage);
    }

    private int GetMaxNumberOfMessagesInABundle(int? maxNumberOfMessagesInABundle, DocumentType documentType)
    {
        if (maxNumberOfMessagesInABundle != null)
            return maxNumberOfMessagesInABundle.Value;

        return documentType.Category == MessageCategory.Aggregations ? 1 : 10000;
    }

    public PeekResult? Peek()
    {
        var bundle = NextBundleToPeek();

        return bundle is not null
            ? new PeekResult(bundle.Id, bundle.MessageId)
            : null;
    }

    public PeekResult? Peek(MessageCategory category)
    {
        var bundle = NextBundleToPeek(category);

        return bundle is not null
            ? new PeekResult(bundle.Id, bundle.MessageId)
            : null;
    }

    public bool Dequeue(MessageId messageId)
    {
        var bundle = _bundles.FirstOrDefault(bundle => bundle.MessageId.Value == messageId.Value && bundle.DequeuedAt is null);
        if (bundle == null)
        {
            return false;
        }

        bundle.Dequeue();
        return true;
    }

    public IReadOnlyCollection<Bundle> GetDequeuedBundles()
    {
        return _bundles.Where(x => x.DequeuedAt is not null).ToList();
    }

    private void EnsureApplicable(OutgoingMessage outgoingMessage)
    {
        if (outgoingMessage.GetActorMessageQueueMetadata().Equals(Receiver) == false)
        {
            throw new ReceiverMismatchException();
        }
    }

    private Bundle? CurrentBundleOf(BusinessReason businessReason, DocumentType messageType, MessageId? relatedToMessageId = null)
    {
        return _bundles.FirstOrDefault(bundle =>
            bundle.ClosedAt is null
            && bundle.DocumentTypeInBundle == messageType
            && bundle.BusinessReason == businessReason
            && bundle.RelatedToMessageId?.Value == relatedToMessageId?.Value);
    }

    private Bundle CreateBundleOf(BusinessReason businessReason, DocumentType messageType, int maxNumberOfMessagesInABundle, Instant created, MessageId? relatedToMessageId = null)
    {
        var bundle = new Bundle(businessReason, messageType, maxNumberOfMessagesInABundle, created, relatedToMessageId);
        _bundles.Add(bundle);
        return bundle;
    }

    private Bundle? NextBundleToPeek(MessageCategory? category = null)
    {
        var nextBundleToPeek = category is not null
            ? _bundles
                .Where(bundle => bundle.DequeuedAt is null && bundle.DocumentTypeInBundle.Category.Equals(category))
                .MinBy(bundle => bundle.Created)
            : _bundles.Where(bundle => bundle.DequeuedAt is null).MinBy(bundle => bundle.Created);

        nextBundleToPeek?.CloseBundle();

        return nextBundleToPeek;
    }
}
