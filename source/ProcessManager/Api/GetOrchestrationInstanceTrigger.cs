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

using Energinet.DataHub.ProcessManagement.Core.Application;
using Energinet.DataHub.ProcessManagement.Core.Domain.OrchestrationInstance;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.ProcessManager.Api;

internal class GetOrchestrationInstanceTrigger(
    ILogger<GetOrchestrationInstanceTrigger> logger,
    IOrchestrationInstanceRepository repository)
{
    private readonly ILogger _logger = logger;
    private readonly IOrchestrationInstanceRepository _repository = repository;

    [Function(nameof(GetOrchestrationInstanceTrigger))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "processmanager/orchestrationinstance/{id:guid}")]
        HttpRequest httpRequest,
        Guid id,
        FunctionContext executionContext)
    {
        // TODO: We currently do not return "NodaTime.Instant" correctly
        var orchestrationInstance = await _repository.GetAsync(new OrchestrationInstanceId(id)).ConfigureAwait(false);

        return new OkObjectResult(orchestrationInstance);
    }
}
