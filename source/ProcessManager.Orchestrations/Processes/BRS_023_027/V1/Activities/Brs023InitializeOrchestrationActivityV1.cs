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

/// <summary>
/// The first activity in the orchestration.
/// It is responsible for updating the status to 'Running'.
/// </summary>
internal class Brs023InitializeOrchestrationActivityV1(
    IClock clock,
    IOrchestrationInstanceProgressRepository progressRepository,
    IUnitOfWork unitOfWork)
{
    private readonly IClock _clock = clock;
    private readonly IOrchestrationInstanceProgressRepository _progressRepository = progressRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    [Function(nameof(Brs023InitializeOrchestrationActivityV1))]
    public async Task Run(
        [ActivityTrigger] Guid orchestrationInstanceId)
    {
        var orchestrationInstance = await _progressRepository
            .GetAsync(new OrchestrationInstanceId(orchestrationInstanceId))
            .ConfigureAwait(false);

        // TODO: For demo purposes we create the steps here; will be refactored to either:
        //  - describing the steps as part of the orchestration description
        //  - describing the steps in the specific BRS handler located in the API
        orchestrationInstance.Steps.Add(new OrchestrationStep(
            orchestrationInstance.Id,
            _clock,
            "Beregning",
            NotifyAggregatedMeasureDataOrchestrationV1.CalculationStepIndex));
        orchestrationInstance.Steps.Add(new OrchestrationStep(
            orchestrationInstance.Id,
            _clock,
            "Besked dannelse",
            NotifyAggregatedMeasureDataOrchestrationV1.EnqueueMessagesStepIndex));

        orchestrationInstance.Lifecycle.TransitionToRunning(_clock);
        await _unitOfWork.CommitAsync().ConfigureAwait(false);

        // TODO: For demo purposes; remove when done
        await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
    }
}
