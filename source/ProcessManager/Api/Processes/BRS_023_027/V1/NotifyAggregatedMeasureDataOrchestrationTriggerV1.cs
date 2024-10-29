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
using NodaTime.Extensions;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace Energinet.DataHub.ProcessManager.Api.Processes.BRS_023_027.V1;

internal class NotifyAggregatedMeasureDataOrchestrationTriggerV1
{
    private readonly ILogger _logger;
    private readonly IOrchestrationInstanceManager _manager;

    public NotifyAggregatedMeasureDataOrchestrationTriggerV1(
        ILogger<NotifyAggregatedMeasureDataOrchestrationTriggerV1> logger,
        IOrchestrationInstanceManager manager)
    {
        _logger = logger;
        _manager = manager;
    }

    [Function(nameof(NotifyAggregatedMeasureDataOrchestrationTriggerV1))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest httpRequest,
        [FromBody] ScheduleOrchestrationInstanceDto<NotifyAggregatedMeasureDataInputV1> dto,
        FunctionContext executionContext)
    {
        // TODO: Server-side validation => Validate "period" is midnight values when given "timezone"
        var orchestrationInstanceId = await _manager.ScheduleNewOrchestrationInstanceAsync(
            name: "BRS_023_027",
            version: 1,
            parameter: dto.Parameter,
            runAt: dto.ScheduledAt.ToInstant())
            .ConfigureAwait(false);

        return new OkObjectResult(orchestrationInstanceId.Value);
    }
}
