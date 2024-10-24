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
using Energinet.DataHub.ProcessManagement.Core.Domain;
using Microsoft.Azure.Functions.Worker;
using NodaTime;

namespace Energinet.DataHub.ProcessManager.Orchestrations.Processes.BRS_023_027.V1.Activities;

internal class DoSomethingActivityV1(
    IClock clock,
    IOrchestrationInstanceProgressRepository progressRepository,
    IUnitOfWork unitOfWork)
{
    private readonly IClock _clock = clock;
    private readonly IOrchestrationInstanceProgressRepository _progressRepository = progressRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    [Function(nameof(DoSomethingActivityV1))]
    public async Task Run(
        [ActivityTrigger] string orchestrationInstanceId)
    {
        var orchestrationInstance = await _progressRepository
            .GetAsync(new OrchestrationInstanceId(Guid.Parse(orchestrationInstanceId)))
            .ConfigureAwait(false);

        orchestrationInstance.StartedAt = _clock.GetCurrentInstant();
        await _unitOfWork.CommitAsync().ConfigureAwait(false);
    }
}
