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

namespace Energinet.DataHub.ProcessManagement.Core.Domain;

/// <summary>
/// Represents the instance of a workflow (orchestration) step.
/// It contains state information about the step, and is linked
/// to the orchestration instance that it is part of.
/// </summary>
public class OrchestrationStep
{
    public OrchestrationStep(
        OrchestrationInstanceId orchestrationInstanceId,
        IClock clock,
        string description,
        int sequence,
        OrchestrationStepId? dependsOn = default)
    {
        OrchestrationInstanceId = orchestrationInstanceId;
        Id = new OrchestrationStepId(Guid.NewGuid());
        Lifecycle = new OrchestrationStepLifecycleState(clock);
        Description = description;
        Sequence = sequence;
        DependsOn = dependsOn;
        CustomState = new OrchestrationStepCustomState(string.Empty);
    }

    /// <summary>
    /// Used by Entity Framework
    /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    // ReSharper disable once UnusedMember.Local -- Used by Entity Framework
    private OrchestrationStep()
    {
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    /// <summary>
    /// The orchestration instance which this step is part of.
    /// </summary>
    public OrchestrationInstanceId OrchestrationInstanceId { get; }

    public OrchestrationStepId Id { get; }

    /// <summary>
    /// The high-level lifecycle states that all orchestration steps can go through.
    /// </summary>
    public OrchestrationStepLifecycleState Lifecycle { get; }

    public string? Description { get; }

    public int Sequence { get; }

    public OrchestrationStepId? DependsOn { get; }

    /// <summary>
    /// Any custom state of the step.
    /// </summary>
    public OrchestrationStepCustomState CustomState { get; }
}
