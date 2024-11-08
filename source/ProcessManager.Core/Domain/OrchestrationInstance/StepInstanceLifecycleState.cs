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

using NodaTime;

namespace Energinet.DataHub.ProcessManagement.Core.Domain.OrchestrationInstance;

public class StepInstanceLifecycleState
{
    internal StepInstanceLifecycleState()
    {
        State = StepInstanceLifecycleStates.Pending;
    }

    public StepInstanceLifecycleStates State { get; private set; }

    public OrchestrationStepTerminationStates? TerminationState { get; private set; }

    /// <summary>
    /// The time when the Process Manager was used from Durable Functions to
    /// transition the state to Running.
    /// </summary>
    public Instant? StartedAt { get; private set; }

    /// <summary>
    /// The time when the Process Manager was used from Durable Functions to
    /// transition the state to Terminated.
    /// </summary>
    public Instant? TerminatedAt { get; private set; }

    public void TransitionToRunning(IClock clock)
    {
        if (State is not StepInstanceLifecycleStates.Pending)
            throw new InvalidOperationException($"Cannot change state from '{State}' to '{StepInstanceLifecycleStates.Running}'.");

        State = StepInstanceLifecycleStates.Running;
        StartedAt = clock.GetCurrentInstant();
    }

    public void TransitionToTerminated(IClock clock, OrchestrationStepTerminationStates terminationState)
    {
        switch (terminationState)
        {
            case OrchestrationStepTerminationStates.Succeeded:
            case OrchestrationStepTerminationStates.Failed:
                if (State is not StepInstanceLifecycleStates.Running)
                    throw new InvalidOperationException($"Cannot change termination state to '{terminationState}' when '{State}'.");
                break;
            default:
                throw new InvalidOperationException($"Unsupported termination state '{terminationState}'.");
        }

        State = StepInstanceLifecycleStates.Terminated;
        TerminationState = terminationState;
        TerminatedAt = clock.GetCurrentInstant();
    }
}
