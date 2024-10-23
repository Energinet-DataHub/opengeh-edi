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
public class DFOrchestrationDescription : OrchestrationDescription
{
    public DFOrchestrationDescription(
        string name,
        int version,
        bool canBeScheduled,
        string functionName)
        : base(
            name,
            version,
            canBeScheduled)
    {
        FunctionName = functionName;

        ParameterDefinition = new();
    }

    /// <summary>
    /// The name of the Durable Functions orchestration implementation.
    /// </summary>
    public string FunctionName { get; }

    /// <summary>
    /// Defines the Durable Functions orchestration input parameter type.
    /// </summary>
    public DFOrchestrationParameterDefinition ParameterDefinition { get; }
}
