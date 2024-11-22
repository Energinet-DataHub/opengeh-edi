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

using System.Dynamic;

namespace Energinet.DataHub.ProcessManager.Api.Model.OrchestrationInstance;

/// <summary>
/// Represents the instance of an orchestration.
/// It contains state information about the instance.
/// </summary>
/// <param name="Id"></param>
/// <param name="Lifecycle">The high-level lifecycle states that all orchestration instances can go through.</param>
/// <param name="ParameterValue">Contains the Durable Functions orchestration input parameter value.</param>
/// <param name="Steps">Workflow steps the orchestration instance is going through.</param>
/// <param name="CustomState">Any custom state of the orchestration instance.</param>
public record OrchestrationInstanceDto(
    Guid Id,
    OrchestrationInstanceLifecycleStateDto Lifecycle,
    ExpandoObject ParameterValue,
    IReadOnlyCollection<StepInstanceDto> Steps,
    string CustomState);
