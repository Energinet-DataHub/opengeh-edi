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
        string description,
        OrchestrationStepId? dependsOn,
        int sequence)
    {
        Id = new OrchestrationStepId(Guid.NewGuid());
        OrchestrationInstanceId = orchestrationInstanceId;

        Description = description;
        DependsOn = dependsOn;
        Sequence = sequence;

        State = new OrchestrationStepState("Created");
    }

    public OrchestrationStepId Id { get; }

    public string? Description { get; }

    public Instant? StartedAt { get; }

    public Instant? ChangedAt { get; }

    public Instant? CompletedAt { get; }

    public OrchestrationStepId? DependsOn { get; }

    public int Sequence { get; }

    /// <summary>
    /// The state of the step.
    /// </summary>
    public OrchestrationStepState? State { get; }

    /// <summary>
    /// The orchestration instance which this step is part of.
    /// </summary>
    public OrchestrationInstanceId OrchestrationInstanceId { get; }
}
