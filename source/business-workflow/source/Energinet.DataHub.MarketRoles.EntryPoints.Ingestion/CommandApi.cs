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
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.MarketRoles.EntryPoints.Ingestion
{
    public class CommandApi
    {
        private readonly ILogger _logger;

        public CommandApi(
            ILogger logger)
        {
            _logger = logger;
        }

        [Function("CommandApi")]
        public async Task<HttpResponseData> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData request,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("CommandApi");
            logger.LogInformation("C# HTTP trigger function processed a request.");

            var command = await RehydrateAsync(request).ConfigureAwait(false);
            if (command == null)
            {
                var errorResponse = request.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync("Missing body").ConfigureAwait(false);
                return errorResponse;
            }

            // TODO: Send to processing queue
            _logger.LogInformation("Processing...");

            var response = request.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            return response;
        }

        private static async Task<RequestChangeOfSupplier?> RehydrateAsync(HttpRequestData request)
        {
            return await request
                .ReadFromJsonAsync<RequestChangeOfSupplier>()
                .ConfigureAwait(false);
        }
    }
}
