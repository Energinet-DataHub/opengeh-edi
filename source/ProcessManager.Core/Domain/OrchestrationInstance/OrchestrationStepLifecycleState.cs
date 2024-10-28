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

public class OrchestrationStepLifecycleState
{
    internal OrchestrationStepLifecycleState(IClock clock)
    {
        CreatedAt = clock.GetCurrentInstant();
        State = OrchestrationStepLifecycleStates.Pending;
    }

    /// <summary>
    /// Used by Entity Framework
    /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    // ReSharper disable once UnusedMember.Local -- Used by Entity Framework
    private OrchestrationStepLifecycleState()
    {
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public OrchestrationStepLifecycleStates State { get; private set; }

    public OrchestrationStepTerminationStates? TerminationState { get; private set; }

    public Instant CreatedAt { get; }

    public Instant? StartedAt { get; private set; }

    public Instant? TerminatedAt { get; private set; }

    public void TransitionToRunning(IClock clock)
    {
        if (State is not OrchestrationStepLifecycleStates.Pending)
            throw new InvalidOperationException($"Cannot change state from '{State}' to '{OrchestrationStepLifecycleStates.Running}'.");

        State = OrchestrationStepLifecycleStates.Running;
        StartedAt = clock.GetCurrentInstant();
    }

    public void TransitionToTerminated(IClock clock, OrchestrationStepTerminationStates terminationState)
    {
        switch (terminationState)
        {
            case OrchestrationStepTerminationStates.Succeeded:
            case OrchestrationStepTerminationStates.Failed:
                if (State is not OrchestrationStepLifecycleStates.Running)
                    throw new InvalidOperationException($"Cannot change termination state to '{terminationState}' when '{State}'.");
                break;
            default:
                throw new InvalidOperationException($"Unsupported termination state '{terminationState}'.");
        }

        State = OrchestrationStepLifecycleStates.Terminated;
        TerminationState = terminationState;
        TerminatedAt = clock.GetCurrentInstant();
    }
}
