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

using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages.Peek;
using Messaging.Domain.SeedWork;

namespace Messaging.Domain.OutgoingMessages;

public class MessageBundle : ValueObject
{
    private MessageBundle()
    {
    }

    private MessageBundle(ActorNumber actorNumber, MessageCategory messageCategory, IEnumerable<EnqueuedMessage> messages)
    {
        Messages = messages.ToList();
    }

    public IReadOnlyList<EnqueuedMessage> Messages { get; } = new List<EnqueuedMessage>();

    public static MessageBundle Empty()
    {
        return new MessageBundle();
    }

    public static MessageBundle Create(ActorNumber actorNumber, MessageCategory messageCategory, IReadOnlyList<EnqueuedMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        EnsureProcessType(messages);
        EnsureReceiverNumberMatches(messages);

        return new MessageBundle(actorNumber, messageCategory, messages);
    }

    private static void EnsureProcessType(IReadOnlyList<EnqueuedMessage> messages)
    {
        var processType = messages[0].ProcessType;
        var messagesNotMatchingProcessType = messages
            .Where(message => message.ProcessType.Equals(processType, StringComparison.OrdinalIgnoreCase) == false)
            .Select(message => message.Id)
            .ToList();

        if (messagesNotMatchingProcessType.Count > 0)
        {
            throw new ProcessTypesDoesNotMatchException(messagesNotMatchingProcessType);
        }
    }

    private static void EnsureReceiverNumberMatches(IReadOnlyList<EnqueuedMessage> messages)
    {
        var receiverNumber = messages[0].ReceiverId;
        var messagesNotMatching = messages
            .Where(message => message.ProcessType.Equals(receiverNumber, StringComparison.OrdinalIgnoreCase) == false)
            .Select(message => message.Id)
            .ToList();

        if (messagesNotMatching.Count > 0)
        {
            throw new ReceiverIdsDoesNotMatchException(messagesNotMatching);
        }
    }
}
