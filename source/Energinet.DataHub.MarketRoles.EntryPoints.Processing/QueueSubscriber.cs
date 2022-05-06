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
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Processing.Infrastructure.Correlation;
using Processing.Infrastructure.Transport;

namespace Energinet.DataHub.MarketRoles.EntryPoints.Processing
{
    public class QueueSubscriber
    {
        private readonly ILogger _logger;
        private readonly ICorrelationContext _correlationContext;
        private readonly MessageExtractor _messageExtractor;
        private readonly IMediator _mediator;

        public QueueSubscriber(
            ILogger logger,
            ICorrelationContext correlationContext,
            MessageExtractor messageExtractor,
            IMediator mediator)
        {
            _logger = logger;
            _correlationContext = correlationContext;
            _messageExtractor = messageExtractor;
            _mediator = mediator;
        }

        [Function("QueueSubscriber")]
        public async Task RunAsync(
            [ServiceBusTrigger("%MARKET_DATA_QUEUE_TOPIC_NAME%", Connection = "MARKET_DATA_QUEUE_CONNECTION_STRING")] byte[] data,
            FunctionContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var message = await _messageExtractor.ExtractAsync(data).ConfigureAwait(false);

            await _mediator.Send(message).ConfigureAwait(false);

            _logger.LogInformation("Dequeued with correlation id: {CorrelationId}", _correlationContext.Id);
        }
    }
}
