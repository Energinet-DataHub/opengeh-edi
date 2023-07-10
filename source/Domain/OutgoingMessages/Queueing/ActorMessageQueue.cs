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

using Domain.Documents;
using Domain.OutgoingMessages.Peek;
using Domain.SeedWork;

namespace Domain.OutgoingMessages.Queueing;

#pragma warning disable CA1711 // This is actually a message queue
public class ActorMessageQueue : Entity
{
    private readonly Guid _id;
    private readonly Receiver _receiver;
    private readonly List<Bundle> _bundles = new();

    private ActorMessageQueue(Receiver receiver)
    {
        _receiver = receiver;
        _id = Guid.NewGuid();
    }

    #pragma warning disable
    private ActorMessageQueue()
    {
    }

    public static ActorMessageQueue CreateFor(Receiver receiver)
    {
        return new ActorMessageQueue(receiver);
    }

    public void Enqueue(OutgoingMessage outgoingMessage, int? maxNumberOfMessagesInABundle = null)
    {
        ArgumentNullException.ThrowIfNull(outgoingMessage);
        EnsureApplicable(outgoingMessage);

        var currentBundle = CurrentBundleOf(BusinessReason.From(outgoingMessage.BusinessReason), outgoingMessage.DocumentType) ??
                            CreateBundleOf(BusinessReason.From(outgoingMessage.BusinessReason), outgoingMessage.DocumentType,
                                SetMaxNumberOfMessagesInABundle(maxNumberOfMessagesInABundle, outgoingMessage.DocumentType));

        currentBundle.Add(outgoingMessage);
    }

    private int SetMaxNumberOfMessagesInABundle(int? maxNumberOfMessagesInABundle, DocumentType documentType)
    {
        if (maxNumberOfMessagesInABundle != null)
            return maxNumberOfMessagesInABundle.Value;

        return documentType.Category == MessageCategory.Aggregations ? 2000 : 10000;
    }

    public PeekResult Peek()
    {
        return new PeekResult(NextBundleToPeek()?.Id, NextBundleToPeek()?.DocumentTypeInBundle);
    }

    public PeekResult Peek(MessageCategory category)
    {
        return new PeekResult(NextBundleToPeek(category)?.Id, NextBundleToPeek(category)?.DocumentTypeInBundle);
    }

    public void Dequeue(BundleId bundleId)
    {
        var bundle = _bundles.SingleOrDefault(bundle => bundle.Id == bundleId && bundle.IsDequeued == false);
        bundle?.Dequeue();
    }

    private void EnsureApplicable(OutgoingMessage outgoingMessage)
    {
        if (outgoingMessage.Receiver.Equals(_receiver) == false)
        {
            throw new ReceiverMismatchException();
        }
    }

    private Bundle? CurrentBundleOf(BusinessReason businessReason, DocumentType messageType)
    {
        return _bundles.FirstOrDefault(bundle =>
            bundle.IsClosed == false
            && bundle.DocumentTypeInBundle == messageType
            && bundle.BusinessReason == businessReason);
    }

    private Bundle CreateBundleOf(BusinessReason businessReason, DocumentType messageType, int maxNumberOfMessagesInABundle)
    {
        var bundle = new Bundle(BundleId.New(), businessReason, messageType, maxNumberOfMessagesInABundle);
        _bundles.Add(bundle);
        return bundle;
    }

    private Bundle? NextBundleToPeek(MessageCategory? category = null)
    {
        var nextBundleToPeek = category is not null ?
            _bundles.FirstOrDefault(bundle => bundle.IsDequeued == false && bundle.DocumentTypeInBundle.Category.Equals(category)) :
            _bundles.FirstOrDefault(bundle => bundle.IsDequeued == false);

        nextBundleToPeek.CloseBundle();

        return nextBundleToPeek;
    }
}
