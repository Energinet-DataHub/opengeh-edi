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

using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Azure.Messaging.ServiceBus;
using B2B.CimMessageAdapter.Messages;
using B2B.Transactions.Configuration;
using B2B.Transactions.IncomingMessages;
using B2B.Transactions.Infrastructure.Configuration.Serialization;

namespace B2B.Transactions.Infrastructure.Transactions
{
    public class MessageQueueDispatcher : IMessageQueueDispatcher
    {
        private const string CorrelationId = "CorrelationID";
        private readonly ISerializer _jsonSerializer;
        private readonly List<ServiceBusMessage> _transactionQueue;
        private readonly ServiceBusSender? _serviceBusSender;
        private readonly ICorrelationContext _correlationContext;

        public MessageQueueDispatcher(ISerializer jsonSerializer, ServiceBusSender? sender, ICorrelationContext correlationContext)
        {
            _serviceBusSender = sender;
            _correlationContext = correlationContext;
            _jsonSerializer = jsonSerializer;
            _transactionQueue = new List<ServiceBusMessage>();
        }

        public Task AddAsync(IncomingMessage message)
        {
            _transactionQueue.Add(CreateMessage(message));
            return Task.CompletedTask;
        }

        public async Task CommitAsync()
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                if (_serviceBusSender != null) await _serviceBusSender.SendMessagesAsync(_transactionQueue).ConfigureAwait(false);
                scope.Complete();
            }
        }

        private ServiceBusMessage CreateMessage(IncomingMessage transaction)
        {
            var json = _jsonSerializer.Serialize(transaction);
            var data = Encoding.UTF8.GetBytes(json);
            var message = new ServiceBusMessage(data);
            message.ApplicationProperties.Add(CorrelationId, _correlationContext.Id);
            return message;
        }
    }
}
