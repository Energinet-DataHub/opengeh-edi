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

namespace Energinet.DataHub.ProcessManagement.Core.Domain.OrchestrationInstance;

/// <summary>
/// High-level lifecycle states that all orchestration instances can go through.
/// </summary>
public enum OrchestrationInstanceLifecycleStates
{
    /// <summary>
    /// Created and waiting to be started.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// The Process Manager has requested the Task Hub to start the Durable Functions orchestration instance.
    /// </summary>
    StartRequested = 2,

    /// <summary>
    /// A Durable Functions activity has transitioned the orchestration instance into running.
    /// </summary>
    Running = 3,

    /// <summary>
    /// A Durable Functions activity has transitioned the orchestration instance into terminated.
    /// See <see cref="OrchestrationInstanceTerminationStates"/> for details.
    /// </summary>
    Terminated = 4,
}

public enum OrchestrationInstanceTerminationStates
{
    Succeeded = 1,

    Failed = 2,

    /// <summary>
    /// A user canceled the orchestration instance.
    /// </summary>
    UserCanceled = 3,
}
