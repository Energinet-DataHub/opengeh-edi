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
using Microsoft.Azure.Functions.Worker;
using NodaTime;

namespace Energinet.DataHub.ProcessManager.Orchestrations.Processes.BRS_023_027.V1.Activities;

internal class Brs023CalculationStepTerminateActivityV1(
    IClock clock,
    IOrchestrationInstanceProgressRepository progressRepository,
    IUnitOfWork unitOfWork)
    : ProgressActivityBase(
        clock,
        progressRepository,
        unitOfWork)
{
    [Function(nameof(Brs023CalculationStepTerminateActivityV1))]
    public async Task Run(
        [ActivityTrigger] Guid orchestrationInstanceId)
    {
        var orchestrationInstance = await ProgressRepository
            .GetAsync(new OrchestrationInstanceId(orchestrationInstanceId))
            .ConfigureAwait(false);

        var step = orchestrationInstance.CurrentStep();
        step.Lifecycle.TransitionToTerminated(Clock, OrchestrationStepTerminationStates.Succeeded);
        await UnitOfWork.CommitAsync().ConfigureAwait(false);

        // TODO: For demo purposes; remove when done
        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
    }
}
