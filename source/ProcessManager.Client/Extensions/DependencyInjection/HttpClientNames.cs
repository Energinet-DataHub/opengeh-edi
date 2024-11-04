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

namespace Energinet.DataHub.ProcessManager.Client.Extensions.DependencyInjection;

/// <summary>
/// Constants used for naming <see cref="HttpClient"/> instances.
/// </summary>
internal static class HttpClientNames
{
    /// <summary>
    /// Http client for the general Api hosted in Process Manager
    /// </summary>
    public const string GeneralApi = "General";

    /// <summary>
    /// Http client for the specific Api hosted in Process Manager Orchestrations.
    /// </summary>
    public const string OrchestrationsApi = "Orchestrations";
}
