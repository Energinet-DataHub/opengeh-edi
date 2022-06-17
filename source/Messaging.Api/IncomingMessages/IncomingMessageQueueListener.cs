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
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Messaging.Api.Configuration;
using Messaging.Application.Configuration;
using Messaging.Application.IncomingMessages;
using Messaging.Infrastructure.Configuration.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Messaging.Api.IncomingMessages
{
    public class IncomingMessageQueueListener
    {
        private readonly ILogger<IncomingMessageQueueListener> _logger;
        private readonly ICorrelationContext _correlationContext;
        private readonly ISerializer _jsonSerializer;
        private readonly IMediator _mediator;

        public IncomingMessageQueueListener(
            ILogger<IncomingMessageQueueListener> logger,
            ICorrelationContext correlationContext,
            ISerializer jsonSerializer,
            IMediator mediator)
        {
            _logger = logger;
            _correlationContext = correlationContext;
            _jsonSerializer = jsonSerializer;
            _mediator = mediator;
        }

        [Function(nameof(IncomingMessageQueueListener))]
        public async Task RunAsync(
            [ServiceBusTrigger("%INCOMING_MESSAGE_QUEUE_NAME%", Connection = "INCOMING_MESSAGE_QUEUE_LISTENER_CONNECTION_STRING")] byte[] data,
            FunctionContext context)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (context == null) throw new ArgumentNullException(nameof(context));

            SetCorrelationIdFromServiceBusMessage(context);

            var byteAsString = Encoding.UTF8.GetString(data);
            _logger.LogInformation($"Received incoming message: {byteAsString}");

            await _mediator.Send(
                    _jsonSerializer.Deserialize<IncomingMessage>(byteAsString))
                .ConfigureAwait(false);

            _logger.LogInformation("B2B transaction dequeued with correlation id: {CorrelationId}", _correlationContext.Id);
        }

        private void SetCorrelationIdFromServiceBusMessage(FunctionContext context)
        {
            context.BindingContext.BindingData.TryGetValue("UserProperties", out var serviceBusMessageMetadata);

            if (serviceBusMessageMetadata is null)
            {
                throw new InvalidOperationException($"Service bus metadata must be specified as User Properties attributes");
            }

            var metadata = _jsonSerializer.Deserialize<ServiceBusMessageMetadata>(serviceBusMessageMetadata.ToString() ?? throw new InvalidOperationException());
            _correlationContext.SetId(metadata.CorrelationID ?? throw new InvalidOperationException("Service bus metadata property CorrelationID is missing"));

            _logger.LogInformation("Dequeued service bus message with correlation id: " + _correlationContext.Id ?? string.Empty);
        }
    }
}
