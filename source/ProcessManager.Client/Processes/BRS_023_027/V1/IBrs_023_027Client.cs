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

namespace Energinet.DataHub.ProcessManager.Client.Processes.BRS_023_027.V1;

/// <summary>
/// Client for using the BRS-023/BRS_027 Process Manager API.
/// </summary>
public interface IBrs_023_027Client
{
    /// <summary>
    /// Start a BRS-023 or BRS-027 calculation and return its id.
    /// </summary>
    public Task<Guid> StartCalculationAsync(
        ScheduleOrchestrationInstanceDto<NotifyAggregatedMeasureDataInputV1> requestDto,
        CancellationToken cancellationToken);
}
