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

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;

#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public class ActorMessageQueue
{
    // Used for persistent actor message queue entity.
    private readonly List<Bundle> _bundles = new();

    /// <summary>
    /// Create new actor message queue for the given <paramref name="receiver"/>
    /// </summary>
    private ActorMessageQueue(Receiver receiver)
    {
        Receiver = receiver;
        Id = ActorMessageQueueId.New();
    }

    public ActorMessageQueueId Id { get; private set; }

    public Receiver Receiver { get; set; }

    #pragma warning disable
    private ActorMessageQueue()
    {
    }

    public static ActorMessageQueue CreateFor(Receiver receiver)
    {
        return new ActorMessageQueue(receiver);
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

    private Bundle? NextBundleToPeek(MessageCategory? category = null)
    {
        var nextBundleToPeek = category is not null
            ? _bundles
                .Where(bundle => bundle.DequeuedAt is null && bundle.DocumentTypeInBundle.Category.Equals(category))
                .MinBy(bundle => bundle.Created)
            : _bundles.Where(bundle => bundle.DequeuedAt is null).MinBy(bundle => bundle.Created);

        nextBundleToPeek?.PeekBundle();

        return nextBundleToPeek;
    }
}
