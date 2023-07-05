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

namespace Domain.OutgoingMessages.Queueing;

#pragma warning disable CA1711 // This is actually a message queue
public class ActorMessageQueue
{
    private readonly Receiver _receiver;
    private readonly BusinessReason _processType;
    private readonly List<Bundle> _bundles = new();

    private ActorMessageQueue(Receiver receiver, BusinessReason processType)
    {
        _receiver = receiver;
        _processType = processType;
    }

    private Bundle? NextBundleToPeek => _bundles.FirstOrDefault(bundle => bundle.IsDequeued == false);

    public static ActorMessageQueue CreateFor(Receiver receiver, BusinessReason processType)
    {
        return new ActorMessageQueue(receiver, processType);
    }

    public void Enqueue(OutgoingMessage outgoingMessage, int maxNumberOfMessagesInABundle = 1)
    {
        ArgumentNullException.ThrowIfNull(outgoingMessage);
        EnsureApplicable(outgoingMessage);
        var currentBundle = CurrentBundleOf(outgoingMessage.DocumentType) ?? CreateBundleOf(outgoingMessage.DocumentType, maxNumberOfMessagesInABundle);
        currentBundle.Add(outgoingMessage);
    }

    public PeekResult Peek()
    {
        return new PeekResult(NextBundleToPeek?.Id, NextBundleToPeek?.MessageTypeInBundle);
    }

    public void Dequeue()
    {
        NextBundleToPeek?.Dequeue();
    }

    private void EnsureApplicable(OutgoingMessage outgoingMessage)
    {
        if (outgoingMessage.Receiver.Equals(_receiver) == false)
        {
            throw new ReceiverMismatchException();
        }

        if (outgoingMessage.BusinessReason.Equals(_processType.Name, StringComparison.OrdinalIgnoreCase) == false)
        {
            throw new ProcessTypeMismatchException();
        }
    }

    private Bundle? CurrentBundleOf(DocumentType messageType)
    {
        return _bundles.FirstOrDefault(bundle => bundle.IsClosed == false && bundle.MessageTypeInBundle == messageType);
    }

    private Bundle CreateBundleOf(DocumentType messageType, int maxNumberOfMessagesInABundle)
    {
        var bundle = new Bundle(BundleId.New(), messageType, maxNumberOfMessagesInABundle);
        _bundles.Add(bundle);
        return bundle;
    }
}
