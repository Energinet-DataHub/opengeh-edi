﻿// Copyright 2020 Energinet DataHub A/S
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

using Energinet.DataHub.ProcessManagement.Core.Application.Orchestration;
using Energinet.DataHub.ProcessManagement.Core.Domain.OrchestrationInstance;
using Energinet.DataHub.ProcessManager.Api.Mappers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.ProcessManager.Api;

internal class GetOrchestrationInstanceTrigger(
    ILogger<GetOrchestrationInstanceTrigger> logger,
    IOrchestrationInstanceQueries queries)
{
    private readonly ILogger _logger = logger;
    private readonly IOrchestrationInstanceQueries _queries = queries;

    /// <summary>
    /// Get orchestration instance.
    /// </summary>
    [Function(nameof(GetOrchestrationInstanceTrigger))]
    public async Task<IActionResult> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "processmanager/orchestrationinstance/{id:guid}")]
        HttpRequest httpRequest,
        Guid id,
        FunctionContext executionContext)
    {
        var orchestrationInstance = await _queries
            .GetAsync(new OrchestrationInstanceId(id))
            .ConfigureAwait(false);

        var dto = orchestrationInstance.MapToDto();
        return new OkObjectResult(dto);
    }
}
