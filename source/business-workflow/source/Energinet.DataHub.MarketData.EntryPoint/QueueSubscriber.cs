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
using GreenEnergyHub.Messaging;
using GreenEnergyHub.Messaging.Dispatching;
using GreenEnergyHub.Messaging.MessageRouting;
using MediatR;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketData.EntryPoint
{
    public class QueueSubscriber
    {
        private readonly IHubMessageTypeMap _resolver;
        private readonly IHubRehydrator _rehydrator;
        private readonly IMediator _mediator;

        public QueueSubscriber(
            IHubMessageTypeMap resolver,
            IHubRehydrator rehydrator,
            IMediator mediator)
        {
            _resolver = resolver;
            _rehydrator = rehydrator;
            _mediator = mediator;
        }

        [FunctionName("QueueSubscriber")]
        public async Task RunAsync(
            [ServiceBusTrigger("%MARKET_DATA_QUEUE_TOPIC_NAME%", Connection = "MARKET_DATA_QUEUE_CONNECTION_STRING")] QueueMessage queueMessage,
            ILogger logger)
        {
            if (queueMessage == null || queueMessage.MessageType == null || queueMessage.Message == null)
            {
                throw new ArgumentNullException(nameof(queueMessage));
            }

            logger.LogInformation($"C# ServiceBus queue trigger function processed message: {queueMessage}");
            logger.LogInformation($"With type: {queueMessage.MessageType}");
            logger.LogInformation($"With value: {queueMessage.Message}");

            var messageType = _resolver.GetTypeByCategory(queueMessage.MessageType);
            if (messageType == null)
            {
                throw new ArgumentException($"Invalid category: {queueMessage.MessageType}");
            }

            var hubRequest = await _rehydrator.RehydrateAsync(queueMessage.Message, messageType).ConfigureAwait(false);
            if (hubRequest == null)
            {
                throw new ArgumentException(nameof(queueMessage.Message));
            }

            await _mediator.Send(hubRequest).ConfigureAwait(false);

            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
