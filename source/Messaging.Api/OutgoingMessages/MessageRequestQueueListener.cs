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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MessageHub.Client.Storage;
using Energinet.DataHub.MessageHub.Model.Peek;
using MediatR;
using Messaging.Application.Configuration;
using Messaging.Application.OutgoingMessages.Requesting;
using Messaging.Infrastructure.Configuration.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Messaging.Api.OutgoingMessages
{
    public class MessageRequestQueueListener
    {
        private readonly ICorrelationContext _correlationContext;
        private readonly ILogger<MessageRequestQueueListener> _logger;
        private readonly IMediator _mediator;
        private readonly IRequestBundleParser _requestBundleParser;
        private readonly IStorageHandler _storageHandler;
        private readonly ISerializer _serializer;

        public MessageRequestQueueListener(
            ICorrelationContext correlationContext,
            ILogger<MessageRequestQueueListener> logger,
            IMediator mediator,
            IRequestBundleParser requestBundleParser,
            IStorageHandler storageHandler,
            ISerializer serializer)
        {
            _correlationContext = correlationContext;
            _logger = logger;
            _mediator = mediator;
            _requestBundleParser = requestBundleParser;
            _storageHandler = storageHandler;
            _serializer = serializer;
        }

        [Function(nameof(MessageRequestQueueListener))]
        public async Task RunAsync([ServiceBusTrigger("%MESSAGE_REQUEST_QUEUE%", Connection = "MESSAGEHUB_QUEUE_CONNECTION_STRING", IsSessionsEnabled = true)] byte[] data)
        {
            var messageRequest = _requestBundleParser.Parse(data);
            _logger.LogInformation($"Requested response format: {messageRequest.ResponseFormat.ToString()}");
            _logger.LogInformation($"Parsed data bundle request DTO: {_serializer.Serialize(messageRequest)}");

            var dataAvailableIds = await _storageHandler.GetDataAvailableNotificationIdsAsync(messageRequest)
                .ConfigureAwait(false);
            _logger.LogInformation($"Parsed message ids: {dataAvailableIds}");

            var clientProvidedDetails = new ClientProvidedDetails(
                messageRequest.RequestId,
                messageRequest.IdempotencyId,
                messageRequest.DataAvailableNotificationReferenceId,
                messageRequest.MessageType.Value,
                messageRequest.ResponseFormat.ToString());

            await _mediator.Send(new RequestMessages(
                dataAvailableIds.Select(x => x.ToString()).ToList() ?? throw new InvalidOperationException(),
                clientProvidedDetails)).ConfigureAwait(false);
            _logger.LogInformation($"Dequeued with correlation id: {_correlationContext.Id}");
        }
    }
}
