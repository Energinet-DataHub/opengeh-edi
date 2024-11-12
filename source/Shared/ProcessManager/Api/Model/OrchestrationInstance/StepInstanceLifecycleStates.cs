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
/// High-level lifecycle states that all orchestration steps can go through.
/// </summary>
public enum StepInstanceLifecycleStates
{
    /// <summary>
    /// Created and waiting to be started.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// A Durable Functions activity has transitioned the orchestration step into running.
    /// </summary>
    Running = 2,

    /// <summary>
    /// A Durable Functions activity has transitioned the orchestration step into terminated.
    /// See <see cref="OrchestrationStepTerminationStates"/> for details.
    /// </summary>
    Terminated = 3,
}

public enum OrchestrationStepTerminationStates
{
    Succeeded = 1,

    Failed = 2,
}
