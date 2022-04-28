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
using B2B.Transactions.Configuration;
using B2B.Transactions.OutgoingMessages;
using Energinet.DataHub.MessageHub.Client.Storage;
using Energinet.DataHub.MessageHub.Model.Peek;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace B2B.Transactions.Api
{
    public class RequestMessageBundleTrigger
    {
        private readonly IRequestBundleParser _requestBundleParser;
        private readonly IStorageHandler _storageHandler;
        private readonly ICorrelationContext _correlationContext;
        private readonly ILogger<RequestMessageBundleTrigger> _logger;
        private readonly MessageRequestHandler _messageRequestHandler;

        public RequestMessageBundleTrigger(
            IRequestBundleParser requestBundleParser,
            IStorageHandler storageHandler,
            ICorrelationContext correlationContext,
            ILogger<RequestMessageBundleTrigger> logger,
            MessageRequestHandler messageRequestHandler)
        {
            _requestBundleParser = requestBundleParser;
            _storageHandler = storageHandler;
            _correlationContext = correlationContext;
            _logger = logger;
            _messageRequestHandler = messageRequestHandler;
        }

        [Function("RequestMessageBundleTrigger")]
        public async Task RunAsync([ServiceBusTrigger("myqueue", Connection = "")] byte[] data)
        {
            var bundleRequestDto = _requestBundleParser.Parse(data);
            var dataAvailableIds = await _storageHandler.GetDataAvailableNotificationIdsAsync(bundleRequestDto)
                .ConfigureAwait(false);
            await _messageRequestHandler.HandleAsync(dataAvailableIds.Select(x => x.ToString()).ToList()).ConfigureAwait(false);

            _logger.LogInformation($"Dequeued with correlation id: {_correlationContext.Id}");
        }
    }
}
