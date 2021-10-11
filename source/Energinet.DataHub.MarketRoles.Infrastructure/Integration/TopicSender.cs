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
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.MarketRoles.Infrastructure.Correlation;

namespace Energinet.DataHub.MarketRoles.Infrastructure.Integration
{
    public class TopicSender<TTopic> : IAsyncDisposable, ITopicSender<TTopic>
        where TTopic : Topic
    {
        private readonly ServiceBusClient _client;
        private readonly Lazy<ServiceBusSender> _senderCreator;
        private readonly string _topicName;

        public TopicSender(ServiceBusClient client, TTopic topic)
        {
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            _client = client ?? throw new ArgumentNullException(nameof(client));
            _senderCreator = new Lazy<ServiceBusSender>(() => _client.CreateSender(topic.Name));
            _topicName = topic.Name;
        }

        public async Task SendMessageAsync(ServiceBusMessage message)
        {
            await _senderCreator.Value.SendMessageAsync(message).ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            await _client.DisposeAsync().ConfigureAwait(false);

            if (_senderCreator.IsValueCreated)
            {
                await _senderCreator.Value.DisposeAsync().ConfigureAwait(false);
            }

            GC.SuppressFinalize(this);
        }
    }
}
