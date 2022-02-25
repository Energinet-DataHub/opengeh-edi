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

using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.Infrastructure.Correlation;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess;
using Energinet.DataHub.MarketRoles.Messaging;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketRoles.EntryPoints.LocalMessageHub.Functions
{
    public class BundleDequeuedQueueSubscriber
    {
        private readonly ILogger _logger;
        private readonly ICorrelationContext _correlationContext;
        private readonly ILocalMessageHubClient _localMessageHubClient;
        private readonly IUnitOfWork _unitOfWork;

        public BundleDequeuedQueueSubscriber(
            ILogger logger,
            ICorrelationContext correlationContext,
            ILocalMessageHubClient localMessageHubClient,
            IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _correlationContext = correlationContext;
            _localMessageHubClient = localMessageHubClient;
            _unitOfWork = unitOfWork;
        }

        [Function("BundleDequeuedQueueSubscriber")]
        public async Task RunAsync([ServiceBusTrigger("%BUNDLE_DEQUEUED_SUBSCRIBER_QUEUE%", Connection = "MESSAGEHUB_QUEUE_CONNECTION_STRING")] byte[] data)
        {
            await _localMessageHubClient.BundleDequeuedAsync(data).ConfigureAwait(false);
            await _unitOfWork.CommitAsync().ConfigureAwait(false);

            _logger.LogInformation("Dequeued with correlation id: {correlationId}", _correlationContext.Id);
        }
    }
}
