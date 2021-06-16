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

using System.Net;
using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.Application.MoveIn;
using Energinet.DataHub.MarketRoles.Infrastructure.Correlation;
using Energinet.DataHub.MarketRoles.Infrastructure.Transport;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketRoles.EntryPoints.Ingestion
{
    public class CommandApi
    {
        private readonly ILogger _logger;
        private readonly MessageDispatcher _messageDispatcher;
        private readonly ICorrelationContext _correlationContext;

        public CommandApi(
            ILogger logger,
            MessageDispatcher messageDispatcher,
            ICorrelationContext correlationContext)
        {
            _logger = logger;
            _messageDispatcher = messageDispatcher;
            _correlationContext = correlationContext;
        }

        [Function("CommandApi")]
        public async Task<HttpResponseData> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData request)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            _logger.LogInformation("Processing...");

            var correlationId = _correlationContext.GetCorrelationId();
            var response = request.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            await response.WriteStringAsync("Correlation id: " + _correlationContext.GetCorrelationId())
                .ConfigureAwait(false);

            await _messageDispatcher.DispatchAsync(new RequestMoveIn(correlationId)).ConfigureAwait(false);

            return response;
        }
    }
}
