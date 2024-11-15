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

using Energinet.DataHub.ProcessManagement.Core.Application.Orchestration;
using Energinet.DataHub.ProcessManagement.Core.Domain.OrchestrationInstance;
using Energinet.DataHub.ProcessManager.Api.Model;
using Energinet.DataHub.ProcessManager.Orchestrations.Processes.BRS_023_027.V1.Model;
using NodaTime.Extensions;

namespace Energinet.DataHub.ProcessManager.Orchestrations.Processes.BRS_023_027.V1;

internal class NotifyAggregatedMeasureDataHandler(
    IStartOrchestrationInstanceCommands manager)
{
    private readonly IStartOrchestrationInstanceCommands _manager = manager;

    public async Task<OrchestrationInstanceId> ScheduleNewCalculationAsync(
        ScheduleOrchestrationInstanceDto<NotifyAggregatedMeasureDataInputV1> dto)
    {
        // TODO:
        // Server-side validation => Validate "period" is midnight values when given "timezone" etc.
        // See class Calculation and method IsValid in Wholesale.

        // Here we show how its possible, based on input, to decide certain steps should be skipped by the orchestration.
        IReadOnlyCollection<int> skipStepsBySequence = dto.InputParameter.IsInternalCalculation
            ? [NotifyAggregatedMeasureDataOrchestrationV1.EnqueueMessagesStepSequence]
            : [];

        var orchestrationInstanceId = await _manager
            .ScheduleNewOrchestrationInstanceAsync(
                name: "BRS_023_027",
                version: 1,
                inputParameter: dto.InputParameter,
                runAt: dto.RunAt.ToInstant(),
                skipStepsBySequence: skipStepsBySequence)
            .ConfigureAwait(false);

        return orchestrationInstanceId;
    }
}
