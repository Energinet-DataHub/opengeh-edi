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
/// Represents the instance of an orchestration.
/// It contains state information about the instance, and is linked
/// to the orchestration description that it is an instance of.
/// </summary>
public class OrchestrationInstance
{
    public OrchestrationInstance(
        OrchestrationDescriptionId orchestrationDescriptionId,
        IClock clock,
        Instant? scheduledToRunAt = default)
    {
        OrchestrationDescriptionId = orchestrationDescriptionId;
        Id = new OrchestrationInstanceId(Guid.NewGuid());
        Lifecycle = new OrchestrationInstanceLifecycleState(clock, scheduledToRunAt);
        ParameterValue = new();
        Steps = [];
        CustomState = new OrchestrationInstanceCustomState(string.Empty);
    }

    /// <summary>
    /// Used by Entity Framework
    /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    // ReSharper disable once UnusedMember.Local -- Used by Entity Framework
    private OrchestrationInstance()
    {
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    /// <summary>
    /// The orchestration description for the Durable Functions orchestration which describes
    /// the workflow that the orchestration instance is an instance of.
    /// </summary>
    public OrchestrationDescriptionId OrchestrationDescriptionId { get; }

    public OrchestrationInstanceId Id { get; }

    /// <summary>
    /// The high-level lifecycle states that all orchestration instances can go through.
    /// </summary>
    public OrchestrationInstanceLifecycleState Lifecycle { get; }

    /// <summary>
    /// Defines the Durable Functions orchestration input parameter value.
    /// </summary>
    public OrchestrationParameterValue ParameterValue { get; }

    /// <summary>
    /// Workflow steps the orchestration instance is going through.
    /// </summary>
    public IList<OrchestrationStep> Steps { get; }

    /// <summary>
    /// Any custom state of the orchestration instance.
    /// </summary>
    public OrchestrationInstanceCustomState CustomState { get; }
}
