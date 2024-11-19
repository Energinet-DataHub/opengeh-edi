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

namespace Energinet.DataHub.ProcessManager.Api.Model.OrchestrationInstance;

/// <summary>
/// The orchestration instance lifecycle state information.
/// </summary>
/// <param name="CreatedBy">The identity that caused this orchestration instance to be created.</param>
/// <param name="State"></param>
/// <param name="TerminationState"></param>
/// <param name="CanceledBy">The identity that caused this orchestration instance to be canceled.</param>
/// <param name="CreatedAt">The time when the orchestration instance was created (State => Pending).</param>
/// <param name="ScheduledToRunAt">The time when the orchestration instance should be executed by the Scheduler.</param>
/// <param name="QueuedAt">The time when the Process Manager has queued the orchestration instance for execution by Durable Functions (State => Queued).</param>
/// <param name="StartedAt">The time when the Process Manager was used from Durable Functions to transition the state to Running.</param>
/// <param name="TerminatedAt">The time when the Process Manager was used from Durable Functions to transition the state to Terminated.</param>
public record OrchestrationInstanceLifecycleStateDto(
    IOperatingIdentityDto CreatedBy,
    OrchestrationInstanceLifecycleStates State,
    OrchestrationInstanceTerminationStates? TerminationState,
    IOperatingIdentityDto? CanceledBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ScheduledToRunAt,
    DateTimeOffset? QueuedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? TerminatedAt);
