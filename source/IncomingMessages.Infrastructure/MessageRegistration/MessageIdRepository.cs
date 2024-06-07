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

using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageRegistration;

public class MessageIdRepository : IMessageIdRepository
{
    private readonly IncomingMessagesContext _incomingMessagesContext;

    public MessageIdRepository(
        IncomingMessagesContext incomingMessagesContext)
    {
        _incomingMessagesContext = incomingMessagesContext;
    }

    public async Task AddAsync(string senderNumber, string messageId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(senderNumber);

        await _incomingMessagesContext.MessageIdForSenders.AddAsync(
                new MessageIdForSender(messageId, senderNumber), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> MessageIdExistsAsync(
        string senderNumber,
        string messageId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(senderNumber);

        var message = await GetMessageFromDbAsync(senderNumber, messageId, cancellationToken).ConfigureAwait(false)
                          ?? GetMessageFromInMemoryCollection(senderNumber, messageId);

        return message != null;
    }

    private MessageIdForSender? GetMessageFromInMemoryCollection(string senderNumber, string messageId)
    {
        return _incomingMessagesContext.MessageIdForSenders.Local
            .FirstOrDefault(x => x.MessageId == messageId && x.SenderId == senderNumber);
    }

    private async Task<MessageIdForSender?> GetMessageFromDbAsync(
        string senderId,
        string messageId,
        CancellationToken cancellationToken)
    {
        return await _incomingMessagesContext.MessageIdForSenders
            .FirstOrDefaultAsync(
                messageIdForSender => messageIdForSender.MessageId == messageId
                                          && messageIdForSender.SenderId == senderId,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }
}
