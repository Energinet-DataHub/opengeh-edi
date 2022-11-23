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

    public async Task<IEnumerable<OutgoingMessage>> GetByAsync(ActorNumber actorNumber)
    {
        ArgumentNullException.ThrowIfNull(actorNumber);
        var sqlStatement = $"SELECT TOP({_bundleConfiguration.MaxNumberOfPayloadsInBundle}) * FROM [b2b].[ActorMessageQueue_{actorNumber.Value}]";
        var messages = await _connectionFactory.GetOpenConnection().QueryAsync<OutgoingMessageEntity>(sqlStatement).ConfigureAwait(false);
        return messages.Select(m => new OutgoingMessage(
            EnumerationType.FromName<DocumentType>(m.DocumentType),
            ActorNumber.Create(m.ReceiverId),
            string.Empty,
            m.ProcessType,
            EnumerationType.FromName<MarketRole>(m.ReceiverRole),
            ActorNumber.Create(m.SenderId),
            EnumerationType.FromName<MarketRole>(m.SenderRole),
            m.Payload));
    }
}

public record OutgoingMessageEntity(int RecordId, Guid Id, string DocumentType, string ReceiverId, string ReceiverRole, string SenderId, string SenderRole, string ProcessType, string Payload);
