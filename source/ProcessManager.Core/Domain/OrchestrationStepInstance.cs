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
public class OrchestrationStepInstance
{
    public OrchestrationStepInstanceId? Id { get; set; }

    public string? Description { get; set; }

    public Instant StartedAt { get; set; }

    public Instant ChangedAt { get; set; }

    public Instant CompletedAt { get; set; }

    public OrchestrationStepInstanceId? DependsOn { get; set; }

    public int Sequence { get; set; }

    /// <summary>
    /// The state of the step.
    /// </summary>
    public OrchestrationStepInstanceState? State { get; set; }

    /// <summary>
    /// The orchestration instance which this step is part of.
    /// </summary>
    public OrchestrationInstanceId? OrchestrationInstanceId { get; set; }
}
