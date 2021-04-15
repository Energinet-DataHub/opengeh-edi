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
using Dapper;
using Energinet.DataHub.MarketData.Application.Common;
using Energinet.DataHub.MarketData.Domain.SeedWork;
using Energinet.DataHub.MarketData.Infrastructure.DataPersistence;
using Energinet.DataHub.MarketData.Infrastructure.Outbox;
using GreenEnergyHub.Json;
using NodaTime;

namespace Energinet.DataHub.MarketData.Infrastructure.ActorMessages
{
    public class ActorMessagePublisher : IActorMessagePublisher, ICanInsertDataModel
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;
        private readonly IUnitOfWorkCallback _unitOfWorkCallback;
        private readonly IJsonSerializer _jsonSerializer;

        public ActorMessagePublisher(IDbConnectionFactory connectionFactory, ISystemDateTimeProvider systemDateTimeProvider, IUnitOfWorkCallback unitOfWorkCallback, IJsonSerializer jsonSerializer)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _systemDateTimeProvider = systemDateTimeProvider ?? throw new ArgumentNullException(nameof(systemDateTimeProvider));
            _unitOfWorkCallback = unitOfWorkCallback ?? throw new ArgumentNullException(nameof(unitOfWorkCallback));
            _jsonSerializer = jsonSerializer;
        }

        public Task PublishAsync<TMessage>(TMessage message, string recipient)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var messageType = message.GetType().Name;
            var payload = _jsonSerializer.Serialize(message);

            var outboxMessage = new OutgoingActorMessage(_systemDateTimeProvider.Now(), messageType, payload, recipient);
            _unitOfWorkCallback.RegisterNew(outboxMessage, this);

            return Task.CompletedTask;
        }

        public async Task PersistCreationOfAsync(IDataModel entity)
        {
            var dataModel = (OutgoingActorMessage)entity;

            if (dataModel is null)
            {
                throw new NullReferenceException(nameof(dataModel));
            }

            var insertStatement = $"INSERT INTO [dbo].[OutgoingActorMessages] (OccurredOn, Type, Data, State, LastUpdatedOn, Recipient) VALUES (@OccurredOn, @Type, @Data, @State, @LastUpdatedOn, @Recipient)";
            await _connectionFactory.GetOpenConnection().ExecuteAsync(insertStatement, new
            {
                OccurredOn = dataModel.OccurredOn,
                Type = dataModel.Type,
                Data = dataModel.Data,
                State = OutboxState.Pending.Id,
                LastUpdatedOn = SystemClock.Instance.GetCurrentInstant(),
                Recipient = dataModel.Recipient,
            }).ConfigureAwait(false);
        }
    }
}
