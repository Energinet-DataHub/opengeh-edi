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

using Messaging.Domain.OutgoingMessages.Peek;
using Messaging.Domain.SeedWork;

namespace Messaging.Domain.OutgoingMessages;

public class MessageBundle : ValueObject
{
    private MessageBundle(IReadOnlyList<EnqueuedMessage> messages)
    {
        if (messages.Count == 0)
        {
            throw new BundleException("A message bundle cannot be empty.");
        }

        if (messages.Count > 1)
        {
            EnsureProcessTypeMatches(messages);
            EnsureReceiverNumberMatches(messages);
            EnsureReceiverRoleMatches(messages);
            EnsureSenderNumberMatches(messages);
            EnsureSenderRoleMatches(messages);
            EnsureMessageTypeMatches(messages);
        }

        Messages = messages;
    }

    public IReadOnlyList<EnqueuedMessage> Messages { get; }

    public string ProcessType => Messages[0].ProcessType;

    public string SenderNumber => Messages[0].SenderId;

    public string SenderRole => Messages[0].SenderRole;

    public string ReceiverNumber => Messages[0].ReceiverId;

    public string ReceiverRole => Messages[0].ReceiverRole;

    public MessageType MessageType => EnumerationType.FromName<MessageType>(Messages[0].MessageType);

    public static MessageBundle Create(IReadOnlyList<EnqueuedMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);
        return new MessageBundle(messages);
    }

    private static void EnsureProcessTypeMatches(IReadOnlyList<EnqueuedMessage> messages)
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
            .Where(message => message.ReceiverId.Equals(receiverNumber, StringComparison.OrdinalIgnoreCase) == false)
            .Select(message => message.Id)
            .ToList();

        if (messagesNotMatching.Count > 0)
        {
            throw new ReceiverIdsDoesNotMatchException(messagesNotMatching);
        }
    }

    private static void EnsureReceiverRoleMatches(IReadOnlyList<EnqueuedMessage> messages)
    {
        var receiverRole = messages[0].ReceiverRole;
        var messagesNotMatching = messages
            .Where(message => message.ReceiverRole.Equals(receiverRole, StringComparison.OrdinalIgnoreCase) == false)
            .Select(message => message.Id)
            .ToList();

        if (messagesNotMatching.Count > 0)
        {
            throw new ReceiverRoleDoesNotMatchException(messagesNotMatching);
        }
    }

    private static void EnsureSenderNumberMatches(IReadOnlyList<EnqueuedMessage> messages)
    {
        var senderNumber = messages[0].SenderId;
        var messagesNotMatching = messages
            .Where(message => message.SenderId.Equals(senderNumber, StringComparison.OrdinalIgnoreCase) == false)
            .Select(message => message.Id)
            .ToList();

        if (messagesNotMatching.Count > 0)
        {
            throw new SenderNumberDoesNotMatchException(messagesNotMatching);
        }
    }

    private static void EnsureSenderRoleMatches(IReadOnlyList<EnqueuedMessage> messages)
    {
        var senderRole = messages[0].SenderRole;
        var messagesNotMatching = messages
            .Where(message => message.SenderRole.Equals(senderRole, StringComparison.OrdinalIgnoreCase) == false)
            .Select(message => message.Id)
            .ToList();

        if (messagesNotMatching.Count > 0)
        {
            throw new SenderRoleDoesNotMatchException(messagesNotMatching);
        }
    }

    private static void EnsureMessageTypeMatches(IReadOnlyList<EnqueuedMessage> messages)
    {
        var messageType = messages[0].MessageType;
        var messagesNotMatching = messages
            .Where(message => message.MessageType.Equals(messageType, StringComparison.OrdinalIgnoreCase) == false)
            .Select(message => message.Id)
            .ToList();

        if (messagesNotMatching.Count > 0)
        {
            throw new MessageTypeDoesNotMatchException(messagesNotMatching);
        }
    }
}
