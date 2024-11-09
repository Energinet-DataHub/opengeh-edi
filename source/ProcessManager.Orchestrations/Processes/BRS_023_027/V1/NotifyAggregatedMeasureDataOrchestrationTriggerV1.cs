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
using Energinet.DataHub.ProcessManager.Api.Model;
using Energinet.DataHub.ProcessManager.Orchestrations.Processes.BRS_023_027.V1.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Extensions;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace Energinet.DataHub.ProcessManager.Orchestrations.Processes.BRS_023_027.V1;

internal class NotifyAggregatedMeasureDataOrchestrationTriggerV1(
    ILogger<NotifyAggregatedMeasureDataOrchestrationTriggerV1> logger,
    IClock clock,
    IOrchestrationInstanceRepository repository,
    IUnitOfWork unitOfWork,
    IOrchestrationInstanceManager manager)
{
    private readonly ILogger _logger = logger;
    private readonly IClock _clock = clock;
    private readonly IOrchestrationInstanceRepository _repository = repository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IOrchestrationInstanceManager _manager = manager;

    /// <summary>
    /// Schedule a BRS-023 or BRS-027 calculation and return its id.
    /// </summary>
    [Function(nameof(NotifyAggregatedMeasureDataOrchestrationTriggerV1))]
    public async Task<IActionResult> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "post",
            Route = "processmanager/orchestrationinstance/brs_023_027/1")]
        HttpRequest httpRequest,
        [FromBody]
        ScheduleOrchestrationInstanceDto<NotifyAggregatedMeasureDataInputV1> dto,
        FunctionContext executionContext)
    {
        // TODO: Server-side validation => Validate "period" is midnight values when given "timezone"
        var orchestrationInstanceId = await _manager
            .ScheduleNewOrchestrationInstanceAsync(
                name: "BRS_023_027",
                version: 1,
                inputParameter: dto.InputParameter,
                runAt: dto.RunAt.ToInstant())
            .ConfigureAwait(false);

        // TODO:
        // If we want to be able to "skip" steps based on input, we should be able to do it here.
        // If we want the UI to have the correct information immediately, then we must be able to first
        // create the orchestration instance, make any modification (e.g. mark step 2 as skip)
        // and THEN call Start or Schedule.

        return new OkObjectResult(orchestrationInstanceId.Value);
    }
}
