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
using Messaging.Application.Configuration;
using Messaging.Application.OutgoingMessages;
using Messaging.Domain.OutgoingMessages;
using Messaging.Infrastructure.OutgoingMessages;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Messaging.Api.OutgoingMessages
{
    public class MessageRequestQueueListener
    {
        private readonly ICorrelationContext _correlationContext;
        private readonly ILogger<MessageRequestQueueListener> _logger;
        private readonly MessageRequestContext _messageRequestContext;
        private readonly IMediator _mediator;

        public MessageRequestQueueListener(
            ICorrelationContext correlationContext,
            ILogger<MessageRequestQueueListener> logger,
            MessageRequestContext messageRequestContext,
            IMediator mediator)
        {
            _correlationContext = correlationContext;
            _logger = logger;
            _messageRequestContext = messageRequestContext;
            _mediator = mediator;
        }

        [Function(nameof(MessageRequestQueueListener))]
        public async Task RunAsync([ServiceBusTrigger("%MESSAGE_REQUEST_QUEUE%", Connection = "MESSAGEHUB_QUEUE_CONNECTION_STRING", IsSessionsEnabled = true)] byte[] data)
        {
            await _messageRequestContext.SetMessageRequestContextAsync(data).ConfigureAwait(false);
            await _mediator.Send(new RequestMessages(
                _messageRequestContext.DataAvailableIds ?? throw new InvalidOperationException(),
                CimFormat.Xml.Name)).ConfigureAwait(false);
            _logger.LogInformation($"Dequeued with correlation id: {_correlationContext.Id}");
        }
    }
}
