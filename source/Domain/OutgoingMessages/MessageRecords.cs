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

namespace Domain.OutgoingMessages;

public class MessageRecords : ValueObject
{
    private readonly IReadOnlyList<EnqueuedMessage> _messages;

    private MessageRecords(IReadOnlyList<EnqueuedMessage> messages)
    {
        if (messages.Count == 0)
        {
            throw new BundleException("A message bundle cannot be empty.");
        }

        if (messages.Count > 1)
        {
            EnsureBusinessReasonMatches(messages);
            EnsureReceiverNumberMatches(messages);
            EnsureReceiverRoleMatches(messages);
            EnsureSenderNumberMatches(messages);
            EnsureSenderRoleMatches(messages);
            EnsureMessageTypeMatches(messages);
        }

        _messages = messages;
    }

    public string BusinessReason => _messages[0].BusinessReason;

    public string SenderNumber => _messages[0].SenderId;

    public string SenderRole => _messages[0].SenderRole;

    public string ReceiverNumber => _messages[0].ReceiverId;

    public string ReceiverRole => _messages[0].ReceiverRole;

    public DocumentType DocumentType => EnumerationType.FromName<DocumentType>(_messages[0].DocumentType);

    public string Category => _messages[0].Category;

    public IReadOnlyList<string> Records => _messages.Select(message => message.MessageRecord).ToList();

    public IEnumerable<Guid> MessageIds => _messages.Select(message => message.Id).AsEnumerable();

    public static MessageRecords Create(IReadOnlyList<EnqueuedMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);
        return new MessageRecords(messages);
    }

    private static void EnsureBusinessReasonMatches(IReadOnlyList<EnqueuedMessage> messages)
    {
        var businessReason = messages[0].BusinessReason;
        var messagesNotMatchingBusinessReason = messages
            .Where(message => message.BusinessReason.Equals(businessReason, StringComparison.OrdinalIgnoreCase) == false)
            .Select(message => message.Id)
            .ToList();

        if (messagesNotMatchingBusinessReason.Count > 0)
        {
            throw new BusinessReasonsDoesNotMatchException(messagesNotMatchingBusinessReason);
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
        var messageType = messages[0].DocumentType;
        var messagesNotMatching = messages
            .Where(message => message.DocumentType.Equals(messageType, StringComparison.OrdinalIgnoreCase) == false)
            .Select(message => message.Id)
            .ToList();

        if (messagesNotMatching.Count > 0)
        {
            throw new MessageTypeDoesNotMatchException(messagesNotMatching);
        }
    }
}
