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

using System.Diagnostics.CodeAnalysis;

namespace Energinet.DataHub.EDI.B2BApi;

[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Property name should match environment name")]
public class RuntimeEnvironment
{
    public static RuntimeEnvironment Default => new();

    public virtual string? AZURE_FUNCTIONS_ENVIRONMENT =>
        GetEnvironmentVariable(nameof(AZURE_FUNCTIONS_ENVIRONMENT));

    protected virtual string? GetEnvironmentVariable(string variable)
        => Environment.GetEnvironmentVariable(variable);
}
