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
using System.Text.Json;

namespace Energinet.DataHub.ProcessManagement.Core.Domain.OrchestrationInstance;

/// <summary>
/// Store a Durable Functions orchestration input parameter value as JSON.
/// </summary>
public class OrchestrationInstanceParameterValue
{
    internal OrchestrationInstanceParameterValue()
    {
        SerializedParameterValue = string.Empty;
    }

    /// <summary>
    /// The JSON representation of the orchestration input parameter value.
    /// </summary>
    public string SerializedParameterValue { get; private set; }

    /// <summary>
    /// Serialize the parameter value from an instance.
    /// An input parameter for Durable Functions orchestration must be a <see langword="class"/>
    /// (which includes <see langword="record"/>), and be serializable to JSON.
    /// </summary>
    public void SetFromInstance<TParameter>(TParameter instance)
        where TParameter : class
    {
        SerializedParameterValue = JsonSerializer.Serialize(instance);
    }

    public ExpandoObject AsExpandoObject()
    {
        return JsonSerializer.Deserialize<ExpandoObject>(SerializedParameterValue) ?? new ExpandoObject();
    }
}
