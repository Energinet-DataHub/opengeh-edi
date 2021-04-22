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
using System.Threading.Tasks;
using Energinet.DataHub.MarketData.Application.Common;
using Energinet.DataHub.MarketData.Domain.SeedWork;
using Energinet.DataHub.MarketData.Infrastructure.DatabaseAccess.Write;
using Energinet.DataHub.MarketData.Infrastructure.Outbox;
using GreenEnergyHub.Json;

namespace Energinet.DataHub.MarketData.Infrastructure.ActorMessages
{
    public class ActorMessagePublisher : IActorMessagePublisher
    {
        private readonly IWriteDatabaseContext _writeDatabaseContext;
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;
        private readonly IJsonSerializer _jsonSerializer;

        public ActorMessagePublisher(IWriteDatabaseContext writeDatabaseContext, ISystemDateTimeProvider systemDateTimeProvider, IJsonSerializer jsonSerializer)
        {
            _writeDatabaseContext = writeDatabaseContext;
            _systemDateTimeProvider = systemDateTimeProvider ?? throw new ArgumentNullException(nameof(systemDateTimeProvider));
            _jsonSerializer = jsonSerializer;
        }

        public async Task PublishAsync<TMessage>(TMessage message, string recipient)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var messageType = message.GetType().Name;
            var payload = _jsonSerializer.Serialize(message);

            var outboxMessage = new OutgoingActorMessage(_systemDateTimeProvider.Now(), messageType, payload, recipient);
            await _writeDatabaseContext.OutgoingActorMessageDataModels.AddAsync(new OutgoingActorMessageDataModel
            {
                Id = outboxMessage.Id,
                Data = outboxMessage.Data,
                Recipient = outboxMessage.Recipient,
                State = outboxMessage.State,
                Type = outboxMessage.Type,
                OccurredOn = outboxMessage.OccurredOn,
                LastUpdatedOn = _systemDateTimeProvider.Now(),
            });
        }
    }
}
