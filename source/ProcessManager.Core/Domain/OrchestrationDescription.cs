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
/// A base class for orchestration descriptions.
/// It contains the information necessary to locate and execute an orchestration.
/// Technology specific orchestrations can inherit from this class and add
/// information necessary to execute that technologies orchestrations.
/// </summary>
public abstract class OrchestrationDescription
{
    public OrchestrationDescription(
        string name,
        int version,
        bool canBeScheduled)
    {
        Name = name;
        Version = version;
        CanBeScheduled = canBeScheduled;
    }

    public OrchestrationDescriptionId? Id { get; private set; }

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
    public string HostName { get; set; }
        = string.Empty;

    /// <summary>
    /// Specifies if the orchestration is enabled and hence can be started.
    /// Can be used to disable obsolete orchestrations that we have removed from code,
    /// but which we cannot delete in the database because we still need the execution history.
    /// </summary>
    public bool IsEnabled { get; set; }
}
