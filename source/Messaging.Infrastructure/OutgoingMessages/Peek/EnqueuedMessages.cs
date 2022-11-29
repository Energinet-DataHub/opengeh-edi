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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.OutgoingMessages.Peek;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.Peek;
using Messaging.Domain.SeedWork;

namespace Messaging.Infrastructure.OutgoingMessages.Peek;

public class EnqueuedMessages : IEnqueuedMessages
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IBundleConfiguration _bundleConfiguration;

    public EnqueuedMessages(IDbConnectionFactory connectionFactory, IBundleConfiguration bundleConfiguration)
    {
        _connectionFactory = connectionFactory;
        _bundleConfiguration = bundleConfiguration;
    }

    public async Task<IEnumerable<EnqueuedMessage>> GetByAsync(ActorNumber actorNumber, MarketRole actorRole, MessageCategory messageCategory)
    {
        ArgumentNullException.ThrowIfNull(messageCategory);
        ArgumentNullException.ThrowIfNull(actorRole);
        ArgumentNullException.ThrowIfNull(actorNumber);

        var oldestMessage = await FindOldestMessageAsync(actorNumber, actorRole, messageCategory).ConfigureAwait(false);

        if (oldestMessage is null)
        {
            return Array.Empty<EnqueuedMessage>();
        }

        var sqlStatement =
            @$"SELECT TOP({_bundleConfiguration.MaxNumberOfPayloadsInBundle})
            Id AS {nameof(EnqueuedMessage.Id)},
            ReceiverId AS {nameof(EnqueuedMessage.ReceiverId)},
            ReceiverRole AS {nameof(EnqueuedMessage.ReceiverRole)},
            SenderId AS {nameof(EnqueuedMessage.SenderId)},
            SenderRole AS {nameof(EnqueuedMessage.SenderRole)},
            DocumentType AS {nameof(EnqueuedMessage.DocumentType)},
            MessageCategory AS {nameof(EnqueuedMessage.Category)},
            ProcessType AS {nameof(EnqueuedMessage.ProcessType)},
            Payload FROM [b2b].[ActorMessageQueue_{actorNumber.Value}]
            WHERE ProcessType = @ProcessType AND ReceiverRole = @ReceiverRole AND DocumentType = @DocumentType AND MessageCategory = @MessageCategory";
        return await _connectionFactory
            .GetOpenConnection()
            .QueryAsync<EnqueuedMessage>(sqlStatement, new
            {
                ReceiverRole = actorRole.Name,
                ProcessType = oldestMessage.ProcessType,
                DocumentType = oldestMessage.DocumentType,
                MessageCategory = messageCategory.Name,
            }).ConfigureAwait(false);
        // return messages.Select(m => new OutgoingMessage(
        //     EnumerationType.FromName<DocumentType>(m.DocumentType),
        //     ActorNumber.Create(m.ReceiverId),
        //     string.Empty,
        //     m.ProcessType,
        //     EnumerationType.FromName<MarketRole>(m.ReceiverRole),
        //     ActorNumber.Create(m.SenderId),
        //     EnumerationType.FromName<MarketRole>(m.SenderRole),
        //     m.Payload));
    }

    private async Task<OldestMessage?> FindOldestMessageAsync(ActorNumber actorNumber, MarketRole actorRole, MessageCategory messageCategory)
    {
        return await _connectionFactory
            .GetOpenConnection()
            .QuerySingleOrDefaultAsync<OldestMessage>(
                @$"SELECT TOP(1) {nameof(OldestMessage.ProcessType)}, {nameof(OldestMessage.DocumentType)} FROM [b2b].[ActorMessageQueue_{actorNumber.Value}]
                WHERE ReceiverRole = @ReceiverRole AND MessageCategory = @MessageCategory",
                new
            {
                ReceiverRole = actorRole.Name,
                MessageCategory = messageCategory.Name,
            })
            .ConfigureAwait(false);
    }

    private record OldestMessage(string ProcessType, string DocumentType);
}

public record OutgoingMessageEntity(int RecordId, Guid Id, string DocumentType, string MessageCategory, string ReceiverId, string ReceiverRole, string SenderId, string SenderRole, string ProcessType, string Payload);
