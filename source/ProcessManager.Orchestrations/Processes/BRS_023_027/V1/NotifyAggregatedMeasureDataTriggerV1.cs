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

using Energinet.DataHub.ProcessManager.Api.Model;
using Energinet.DataHub.ProcessManager.Orchestrations.Processes.BRS_023_027.V1.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace Energinet.DataHub.ProcessManager.Orchestrations.Processes.BRS_023_027.V1;

internal class NotifyAggregatedMeasureDataTriggerV1(
    NotifyAggregatedMeasureDataHandler handler)
{
    private readonly NotifyAggregatedMeasureDataHandler _handler = handler;

    /// <summary>
    /// Schedule a BRS-023 or BRS-027 calculation and return its id.
    /// </summary>
    [Function(nameof(NotifyAggregatedMeasureDataTriggerV1))]
    public async Task<IActionResult> Run(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "post",
            Route = "processmanager/orchestrationinstance/brs_023_027/1")]
        HttpRequest httpRequest,
        [FromBody]
        ScheduleOrchestrationInstanceCommand<NotifyAggregatedMeasureDataInputV1> command,
        FunctionContext executionContext)
    {
        var orchestrationInstanceId = await _handler.ScheduleNewCalculationAsync(command).ConfigureAwait(false);
        return new OkObjectResult(orchestrationInstanceId.Value);
    }
}
