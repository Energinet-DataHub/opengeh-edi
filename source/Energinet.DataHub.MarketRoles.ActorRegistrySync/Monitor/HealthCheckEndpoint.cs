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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Energinet.DataHub.MarketRoles.ActorRegistrySync.Monitor
{
    public static class HealthCheckEndpoint
    {
        [FunctionName("HealthCheck")]
        public static Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "monitor/{endpoint}")]
            HttpRequest httpRequest,
            string endpoint)
        {
            if (httpRequest == null) throw new ArgumentNullException(nameof(httpRequest));
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));

            return Task.FromResult<IActionResult>(new OkObjectResult("Ok"));
        }
    }
}
