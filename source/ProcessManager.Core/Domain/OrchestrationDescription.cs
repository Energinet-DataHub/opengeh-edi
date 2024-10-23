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
    public OrchestrationDescriptionId? Id { get; set; }

    public string? Name { get; set; }

    public int Version { get; set; }

    /// <summary>
    /// Flavor text describing an orchestration for end users.
    /// </summary>
    public string? Description { get; set; }

    public bool CanBeScheduled { get; set; }

    public IList<OrchestrationParameterDefinition> Parameters { get; }
        = [];
}
