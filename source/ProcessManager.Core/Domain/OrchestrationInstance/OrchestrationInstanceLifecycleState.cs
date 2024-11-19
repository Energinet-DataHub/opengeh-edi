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
    internal OrchestrationInstanceLifecycleState(OperatingIdentity createdBy, IClock clock, Instant? runAt)
    {
        CreatedBy = createdBy;
        CreatedAt = clock.GetCurrentInstant();
        ScheduledToRunAt = runAt;

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

    /// <summary>
    /// The identity that caused this orchestration instance to be created.
    /// </summary>
    public OperatingIdentity CreatedBy { get; }

    /// <summary>
    /// The time when the orchestration instance was created (State => Pending).
    /// </summary>
    public Instant CreatedAt { get; }

    /// <summary>
    /// The time when the orchestration instance should be executed by the Scheduler.
    /// </summary>
    public Instant? ScheduledToRunAt { get; }

    /// <summary>
    /// The time when the Process Manager has queued the orchestration instance
    /// for execution by Durable Functions (State => Queued).
    /// </summary>
    public Instant? QueuedAt { get; private set; }

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

    /// <summary>
    /// The identity that caused this orchestration instance to be canceled.
    /// </summary>
    public OperatingIdentity? CanceledBy { get; private set; }

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

    public void TransitionToSucceeded(IClock clock)
    {
        TransitionToTerminated(clock, OrchestrationInstanceTerminationStates.Succeeded);
    }

    public void TransitionToFailed(IClock clock)
    {
        TransitionToTerminated(clock, OrchestrationInstanceTerminationStates.Failed);
    }

    public void TransitionToUserCanceled(IClock clock, UserIdentity userIdentity)
    {
        TransitionToTerminated(clock, OrchestrationInstanceTerminationStates.UserCanceled, userIdentity);
    }

    private void TransitionToTerminated(IClock clock, OrchestrationInstanceTerminationStates terminationState, UserIdentity? userIdentity = default)
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
                CanceledBy = userIdentity
                    ?? throw new InvalidOperationException("User identity must be specified.");
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
