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

using System.ComponentModel.DataAnnotations;

namespace Energinet.DataHub.ProcessManager.Client.Extensions.Options;

/// <summary>
/// Options for the configuration of Process Manager HTTP clients using the Process Manager API.
/// </summary>
public class ProcessManagerHttpClientsOptions
{
    public const string SectionName = "ProcessManagerHttpClients";

    /// <summary>
    /// Address to the general Api hosted in Process Manager.
    /// </summary>
    [Required]
    public string GeneralApiBaseAddress { get; set; } = string.Empty;

    /// <summary>
    /// Address to the specific Api hosted in Process Manager Orchestrations.
    /// </summary>
    [Required]
    public string OrchestrationsApiBaseAddress { get; set; } = string.Empty;
}
