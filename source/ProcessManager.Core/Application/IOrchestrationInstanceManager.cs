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

using Energinet.DataHub.ProcessManagement.Core.Domain.OrchestrationInstance;
using NodaTime;

namespace Energinet.DataHub.ProcessManagement.Core.Application;

public interface IOrchestrationInstanceManager
{
    /// <summary>
    /// Start a new instance of an orchestration.
    /// </summary>
    Task<OrchestrationInstanceId> StartNewOrchestrationInstanceAsync<TParameter>(
        string name,
        int version,
        TParameter inputParameter,
        IReadOnlyCollection<int> skipStepsBySequence)
            where TParameter : class;

    /// <summary>
    /// Schedule a new instance of an orchestration.
    /// </summary>
    Task<OrchestrationInstanceId> ScheduleNewOrchestrationInstanceAsync<TParameter>(
        string name,
        int version,
        TParameter inputParameter,
        Instant runAt,
        IReadOnlyCollection<int> skipStepsBySequence)
            where TParameter : class;

    /// <summary>
    /// Cancel a scheduled orchestration instance.
    /// </summary>
    Task CancelScheduledOrchestrationInstanceAsync(OrchestrationInstanceId id);
}
