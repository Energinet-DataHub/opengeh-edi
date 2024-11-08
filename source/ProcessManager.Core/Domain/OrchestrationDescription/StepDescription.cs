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

namespace Energinet.DataHub.ProcessManagement.Core.Domain.OrchestrationDescription;

/// <summary>
/// Describes an orchestration step that should be visible to the user
/// (e.g. shown in the UI).
/// It is linked to the orchestration description that it is part of.
/// </summary>
public class StepDescription
{
    internal StepDescription(
        OrchestrationDescriptionId orchestrationDescriptionId,
        string description,
        int sequence)
    {
        Id = new StepDescriptionId(Guid.NewGuid());
        Description = description;
        Sequence = sequence;

        OrchestrationDescriptionId = orchestrationDescriptionId;
    }

    /// <summary>
    /// Used by Entity Framework
    /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    // ReSharper disable once UnusedMember.Local -- Used by Entity Framework
    private StepDescription()
    {
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public StepDescriptionId Id { get; }

    public string Description { get; }

    /// <summary>
    /// The steps number in the list of steps.
    /// The sequence of the first step in the list is 1.
    /// </summary>
    public int Sequence { get; }

    /// <summary>
    /// The orchestration description which this step is part of.
    /// </summary>
    internal OrchestrationDescriptionId OrchestrationDescriptionId { get; }
}
