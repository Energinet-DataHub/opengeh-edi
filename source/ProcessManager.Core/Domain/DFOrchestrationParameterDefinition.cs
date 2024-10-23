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

using System.Text.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;

namespace Energinet.DataHub.ProcessManagement.Core.Domain;

/// <summary>
/// Defines a Durable Functions orchestration input parameter type using a JSON schema.
/// </summary>
public class DFOrchestrationParameterDefinition
{
    internal DFOrchestrationParameterDefinition()
    {
        SerializedParameterDefinition = string.Empty;
    }

    /// <summary>
    /// The JSON schema defining the parameter type.
    /// </summary>
    private string SerializedParameterDefinition { get; set; }

    /// <summary>
    /// Set the parameter definition by specifying its type.
    /// An input parameter for Durable Functions orchestration must be a <see langword="class"/>
    /// (which includes <see langword="record"/>), and be serializable to JSON.
    /// </summary>
    public void SetFromType<TParameter>()
        where TParameter : class
    {
        var schemaGenerator = new JSchemaGenerator();
        var jsonSchema = schemaGenerator.Generate(typeof(TParameter));

        SerializedParameterDefinition = jsonSchema.ToString();
    }

    /// <summary>
    /// Validate <paramref name="parameterValue"/> against the defining JSON schema.
    /// </summary>
    public bool IsValidParameterValue(object parameterValue)
    {
        ArgumentNullException.ThrowIfNull(parameterValue);

        var serializedParameterValue = JsonSerializer.Serialize(parameterValue);

        return IsValidParameterValue(serializedParameterValue);
    }

    /// <summary>
    /// Validate <paramref name="serializedParameterValue"/> against the defining JSON schema.
    /// </summary>
    public bool IsValidParameterValue(string serializedParameterValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serializedParameterValue);

        var jsonSchema = JSchema.Parse(SerializedParameterDefinition);
        var parameterValue = JObject.Parse(serializedParameterValue);

        return parameterValue.IsValid(jsonSchema);
    }
}
