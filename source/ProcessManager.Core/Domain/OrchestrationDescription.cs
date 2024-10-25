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

namespace Energinet.DataHub.ProcessManagement.Core.Domain;

/// <summary>
/// Durable Functions orchestration description.
/// It contains the information necessary to locate and execute a Durable Functions
/// orchestration.
/// </summary>
public class OrchestrationDescription
{
    public OrchestrationDescription(
        string name,
        int version,
        bool canBeScheduled,
        string functionName)
    {
        Id = new OrchestrationDescriptionId(Guid.NewGuid());
        Name = name;
        Version = version;
        CanBeScheduled = canBeScheduled;

        FunctionName = functionName;
        ParameterDefinition = new();

        HostName = string.Empty;
    }

    /// <summary>
    /// Used by Entity Framework
    /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    // ReSharper disable once UnusedMember.Local -- Used by Entity Framework
    private OrchestrationDescription()
    {
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public OrchestrationDescriptionId Id { get;  }

    /// <summary>
    /// A name which combined with the <see cref="Version"/> uniquely identifies the orchestration.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// A version which combined with the <see cref="Name"/> uniquely identifies the orchestration.
    /// </summary>
    public int Version { get; }

    /// <summary>
    /// Specifies if the orchestration supports scheduling.
    /// If <see langword="false"/> then the orchestration can only
    /// be started directly (on-demand) and doesn't support scheduling.
    /// </summary>
    public bool CanBeScheduled { get; }

    /// <summary>
    /// The name of the host where the orchestration is implemented.
    /// </summary>
    public string HostName { get; internal set; }

    /// <summary>
    /// Specifies if the orchestration is enabled and hence can be started.
    /// Can be used to disable obsolete orchestrations that we have removed from code,
    /// but which we cannot delete in the database because we still need the execution history.
    /// </summary>
    public bool IsEnabled { get; internal set; }

    /// <summary>
    /// The name of the Durable Functions orchestration implementation.
    /// </summary>
    public string FunctionName { get; }

    /// <summary>
    /// Defines the Durable Functions orchestration input parameter type.
    /// </summary>
    public OrchestrationParameterDefinition ParameterDefinition { get; }
}
