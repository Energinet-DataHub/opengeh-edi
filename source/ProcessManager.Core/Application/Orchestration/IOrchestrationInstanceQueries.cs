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

using Energinet.DataHub.ProcessManagement.Core.Domain.OrchestrationInstance;
using NodaTime;

namespace Energinet.DataHub.ProcessManagement.Core.Application.Orchestration;

public interface IOrchestrationInstanceQueries
{
    /// <summary>
    /// Get existing orchestration instance.
    /// </summary>
    Task<OrchestrationInstance> GetAsync(OrchestrationInstanceId id);

    /// <summary>
    /// Get all orchestration instances filtered by their related orchestration definition name and version,
    /// and their lifecycle / termination states.
    /// </summary>
    Task<IReadOnlyCollection<OrchestrationInstance>> SearchAsync(
        string name,
        int? version,
        OrchestrationInstanceLifecycleStates? lifecycleState,
        OrchestrationInstanceTerminationStates? terminationState,
        Instant? startedAtOrLater,
        Instant? terminatedAtOrEarlier);
}