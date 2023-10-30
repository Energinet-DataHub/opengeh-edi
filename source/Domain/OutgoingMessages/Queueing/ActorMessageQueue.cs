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

using Energinet.DataHub.EDI.Domain.Common;
using Energinet.DataHub.EDI.Domain.Documents;
using NodaTime;

namespace Energinet.DataHub.EDI.Domain.OutgoingMessages.Queueing;

#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class ActorMessageQueue : Entity
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

        var currentBundle = CurrentBundleOf(BusinessReason.From(outgoingMessage.BusinessReason), outgoingMessage.DocumentType) ??
                            CreateBundleOf(BusinessReason.From(outgoingMessage.BusinessReason), outgoingMessage.DocumentType, 
                                SetMaxNumberOfMessagesInABundle(maxNumberOfMessagesInABundle, outgoingMessage.DocumentType), timeStamp);

        currentBundle.Add(outgoingMessage);
    }

    private int SetMaxNumberOfMessagesInABundle(int? maxNumberOfMessagesInABundle, DocumentType documentType)
    {
        if (maxNumberOfMessagesInABundle != null)
            return maxNumberOfMessagesInABundle.Value;

        return documentType.Category == MessageCategory.Aggregations ? 1 : 10000;
    }

    public PeekResult Peek()
    {
        return new PeekResult(NextBundleToPeek()?.Id, NextBundleToPeek()?.DocumentTypeInBundle);
    }

    public PeekResult Peek(MessageCategory category)
    {
        return new PeekResult(NextBundleToPeek(category)?.Id, NextBundleToPeek(category)?.DocumentTypeInBundle);
    }

    public bool Dequeue(BundleId bundleId)
    {
        var bundle = _bundles.FirstOrDefault(bundle => bundle.Id == bundleId && bundle.IsDequeued == false);
        if (bundle == null)
        {
            return false;
        }

        bundle.Dequeue();
        return true;
    }

    private void EnsureApplicable(OutgoingMessage outgoingMessage)
    {
        if (outgoingMessage.Receiver.Equals(Receiver) == false)
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

    private Bundle CreateBundleOf(BusinessReason businessReason, DocumentType messageType, int maxNumberOfMessagesInABundle, Instant timeStamp)
    {
        var bundle = new Bundle(BundleId.New(), businessReason, messageType, maxNumberOfMessagesInABundle, timeStamp);
        _bundles.Add(bundle);
        return bundle;
    }

    private Bundle? NextBundleToPeek(MessageCategory? category = null)
    {
        var nextBundleToPeek = category is not null ?
            _bundles.Where(bundle => !bundle.IsDequeued && bundle.DocumentTypeInBundle.Category.Equals(category)).OrderBy(bundle => bundle.Created).FirstOrDefault() :
            _bundles.Where(bundle => !bundle.IsDequeued).OrderBy(bundle => bundle.Created).FirstOrDefault();

        nextBundleToPeek?.CloseBundle();

        return nextBundleToPeek;
    }
}
