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

public class OrchestrationInstanceLifecycleState
{
    internal OrchestrationInstanceLifecycleState(IClock clock, Instant? scheduledToRunAt)
    {
        CreatedAt = clock.GetCurrentInstant();
        ScheduledToRunAt = scheduledToRunAt;

        State = OrchestrationInstanceLifecycleStates.Pending;
    }

    /// <summary>
    /// Used by Entity Framework
    /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    // ReSharper disable once UnusedMember.Local -- Used by Entity Framework
    private OrchestrationInstanceLifecycleState()
    {
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public OrchestrationInstanceLifecycleStates State { get; private set; }

    public OrchestrationInstanceTerminationStates? TerminationState { get; private set; }

    public Instant CreatedAt { get; }

    public Instant? ScheduledToRunAt { get; }

    public Instant? QueuedAt { get; private set; }

    public Instant? StartedAt { get; private set; }

    public Instant? TerminatedAt { get; private set; }

    public bool IsPendingForScheduledStart()
    {
        return
            State == OrchestrationInstanceLifecycleStates.Pending
            && ScheduledToRunAt.HasValue;
    }

    public void TransitionToQueued(IClock clock)
    {
        if (State is not OrchestrationInstanceLifecycleStates.Pending)
            ThrowInvalidStateTransitionException(State, OrchestrationInstanceLifecycleStates.Queued);

        State = OrchestrationInstanceLifecycleStates.Queued;
        QueuedAt = clock.GetCurrentInstant();
    }

    public void TransitionToRunning(IClock clock)
    {
        if (State is not OrchestrationInstanceLifecycleStates.Queued)
            ThrowInvalidStateTransitionException(State, OrchestrationInstanceLifecycleStates.Running);

        State = OrchestrationInstanceLifecycleStates.Running;
        StartedAt = clock.GetCurrentInstant();
    }

    public void TransitionToTerminated(IClock clock, OrchestrationInstanceTerminationStates terminationState)
    {
        switch (terminationState)
        {
            case OrchestrationInstanceTerminationStates.Succeeded:
            case OrchestrationInstanceTerminationStates.Failed:
                if (State is not OrchestrationInstanceLifecycleStates.Running)
                    throw new InvalidOperationException($"Cannot change termination state to '{terminationState}' when '{State}'.");
                break;
            case OrchestrationInstanceTerminationStates.UserCanceled:
                if (!IsPendingForScheduledStart())
                    throw new InvalidOperationException("User cannot cancel orchestration instance.");
                break;
            default:
                throw new InvalidOperationException($"Unsupported termination state '{terminationState}'.");
        }

        State = OrchestrationInstanceLifecycleStates.Terminated;
        TerminationState = terminationState;
        TerminatedAt = clock.GetCurrentInstant();
    }

    private void ThrowInvalidStateTransitionException(
        OrchestrationInstanceLifecycleStates currentState,
        OrchestrationInstanceLifecycleStates desiredState)
    {
        throw new InvalidOperationException($"Cannot change state from '{State}' to '{desiredState}'.");
    }
}
