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
using Messaging.Api.Configuration;
using Messaging.Application.Configuration;
using Messaging.Application.OutgoingMessages;
using Messaging.Infrastructure.Configuration.Serialization;
using Messaging.Infrastructure.OutgoingMessages;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Messaging.Api.OutgoingMessages
{
    public class MessageRequestQueueListener
    {
        private readonly ICorrelationContext _correlationContext;
        private readonly ILogger<MessageRequestQueueListener> _logger;
        private readonly MessageRequestHandler _messageRequestHandler;
        private readonly MessageRequestContext _messageRequestContext;
        private readonly ISerializer _serializer;

        public MessageRequestQueueListener(
            ICorrelationContext correlationContext,
            ILogger<MessageRequestQueueListener> logger,
            MessageRequestHandler messageRequestHandler,
            MessageRequestContext messageRequestContext,
            ISerializer serializer)
        {
            _correlationContext = correlationContext;
            _logger = logger;
            _messageRequestHandler = messageRequestHandler;
            _messageRequestContext = messageRequestContext;
            _serializer = serializer;
        }

        [Function(nameof(MessageRequestQueueListener))]
        public async Task RunAsync([ServiceBusTrigger("%MESSAGE_REQUEST_QUEUE%", Connection = "MESSAGEHUB_QUEUE_CONNECTION_STRING", IsSessionsEnabled = true)] byte[] data)
        {
            await _messageRequestContext.SetMessageRequestContextAsync(data).ConfigureAwait(false);
            var result = await _messageRequestHandler.HandleAsync(
                _messageRequestContext.DataAvailableIds
                ?? throw new InvalidOperationException()).ConfigureAwait(false);

            _logger.LogInformation($"Dequeued with correlation id: {_correlationContext.Id}");
        }

        private void SetCorrelationIdFromServiceBusMessage(FunctionContext context)
        {
            context.BindingContext.BindingData.TryGetValue("UserProperties", out var serviceBusMessageMetadata);

            if (serviceBusMessageMetadata is null)
            {
                throw new InvalidOperationException($"Service bus metadata must be specified as User Properties attributes");
            }

            var metadata = _serializer.Deserialize<ServiceBusMessageMetadata>(serviceBusMessageMetadata.ToString() ?? throw new InvalidOperationException());
            _correlationContext.SetId(metadata.CorrelationID ?? throw new InvalidOperationException("Service bus metadata property CorrelationID is missing"));

            _logger.LogInformation("Dequeued service bus message with correlation id: " + _correlationContext.Id ?? string.Empty);
        }
    }
}
