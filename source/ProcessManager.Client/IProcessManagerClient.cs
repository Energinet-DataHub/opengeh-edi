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

using Energinet.DataHub.ProcessManager.Api.Model.OrchestrationInstance;

namespace Energinet.DataHub.ProcessManager.Client;

/// <summary>
/// Client for using the generic Process Manager API.
/// </summary>
public interface IProcessManagerClient
{
    /// <summary>
    /// Cancel a scheduled orchestration instance.
    /// </summary>
    public Task CancelScheduledOrchestrationInstanceAsync(
        Guid id,
        CancellationToken cancellationToken);

    /// <summary>
    /// Get orchestration instance.
    /// </summary>
    public Task<OrchestrationInstanceDto> GetOrchestrationInstanceAsync(
        Guid id,
        CancellationToken cancellationToken);

    /// <summary>
    /// Get all orchestration instances filtered by their related orchestration definition name and version,
    /// and their lifecycle / termination states.
    /// </summary>
    public Task<IReadOnlyCollection<OrchestrationInstanceDto>> SearchOrchestrationsInstanceAsync(
        string name,
        int? version,
        OrchestrationInstanceLifecycleStates? lifecycleState,
        OrchestrationInstanceTerminationStates? terminationState,
        CancellationToken cancellationToken);
}
