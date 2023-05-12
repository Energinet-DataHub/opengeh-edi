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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Application.Configuration;
using Application.IncomingMessages;
using Azure.Messaging.ServiceBus;
using CimMessageAdapter.Messages;
using CimMessageAdapter.Messages.Queues;
using Infrastructure.Configuration.MessageBus;
using Infrastructure.Configuration.Serialization;

namespace Infrastructure.IncomingMessages
{
    public class MessageQueueDispatcher<TQueue> : IMessageQueueDispatcher<TQueue>
    where TQueue : Queue
    {
        private const string CorrelationId = "CorrelationID";
        private readonly ISerializer _jsonSerializer;
        private readonly List<ServiceBusMessage> _transactionQueue;
        private readonly IServiceBusSenderAdapter _senderCreator;
        private readonly ICorrelationContext _correlationContext;
        private readonly IServiceBusSenderFactory _serviceBusSenderFactory;

        public MessageQueueDispatcher(ISerializer jsonSerializer, ICorrelationContext correlationContext, TQueue queue, IServiceBusSenderFactory serviceBusSenderFactory)
        {
            if (queue == null) throw new ArgumentNullException(nameof(queue));

            _correlationContext = correlationContext;
            _serviceBusSenderFactory = serviceBusSenderFactory;
            _jsonSerializer = jsonSerializer;
            _senderCreator = _serviceBusSenderFactory.GetSender(queue.Name);
            _transactionQueue = new List<ServiceBusMessage>();
        }

        public Task AddAsync(IMarketTransaction message, CancellationToken cancellationToken)
        {
            _transactionQueue.Add(CreateMessage(message));
            return Task.CompletedTask;
        }

        public async Task CommitAsync(CancellationToken cancellationToken)
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                await _senderCreator.SendAsync(_transactionQueue.AsReadOnly(), cancellationToken).ConfigureAwait(false);
                scope.Complete();
            }
        }

        private ServiceBusMessage CreateMessage(IMarketTransaction transaction)
        {
            var json = _jsonSerializer.Serialize(transaction);
            var data = Encoding.UTF8.GetBytes(json);
            var message = new ServiceBusMessage(data);
            message.ApplicationProperties.Add(CorrelationId, _correlationContext.Id);
            return message;
        }
    }
}
