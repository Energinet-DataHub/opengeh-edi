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
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.MarketRoles.Application.Common.Transport;
using Energinet.DataHub.MarketRoles.Infrastructure.Integration;
using Energinet.DataHub.MarketRoles.Infrastructure.Serialization;
using Newtonsoft.Json;

namespace B2B.CimMessageAdapter.MarketActivity
{
    public class MarketActivityRecordForwarder : IMarketActivityRecordForwarder
    {
        private readonly ITopicSender<MarketActivityRecordTopic> _topicSender;
        private readonly List<MarketActivityRecord> _uncommittedItems = new();
        private readonly List<MarketActivityRecord> _committedItems = new();
        private readonly List<ServiceBusMessage> _transactionItems = new();
        private readonly IJsonSerializer _jsonSerializer;

        public IReadOnlyCollection<MarketActivityRecord> CommittedItems => _committedItems.AsReadOnly();

        [SuppressMessage(
            "StyleCop.CSharp.OrderingRules",
            "SA1201:Elements should appear in the correct order",
            Justification = "Disallowing properties before a constructor is stupid")]
        public MarketActivityRecordForwarder(ITopicSender<MarketActivityRecordTopic> topicSender, IJsonSerializer jsonSerializer)
        {
            _topicSender = topicSender;
            _jsonSerializer = jsonSerializer;
        }

        public Task AddAsync(MarketActivityRecord marketActivityRecord)
        {
            _committedItems.Clear();
            _uncommittedItems.Add(marketActivityRecord);
            return Task.CompletedTask;
        }

        public async Task CommitAsync()
        {
            _committedItems.Clear();
            _committedItems.AddRange(_uncommittedItems);
            _uncommittedItems.Clear();

            foreach (var item in _committedItems)
            {
                var message = CreateMessage(item);
                _transactionItems.Add(message);
            }

            await HandleTransactionsAsync(_transactionItems).ConfigureAwait(false);
        }

        private ServiceBusMessage CreateMessage(MarketActivityRecord item)
        {
            var json = _jsonSerializer.Serialize(item);
            var data = Encoding.UTF8.GetBytes(json);
            var message = new ServiceBusMessage(data);
            return message;
        }

        private async Task HandleTransactionsAsync(List<ServiceBusMessage> messages)
        {
            using (var transactionScope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                foreach (var message in messages)
                {
                    await _topicSender.SendMessageAsync(message).ConfigureAwait(false);
                }

                transactionScope.Complete();
            }
        }
    }
}
