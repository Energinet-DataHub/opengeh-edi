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

namespace Energinet.DataHub.ProcessManagement.Core.Infrastructure.Extensions.Options;

/// <summary>
/// Applications that participate in Durable Functions orchestration, either by containing orchestrations
/// or by managing them (e.g. trigger an orchestration), must use the same Task Hub.
/// The configuration of the Task Hub is performed in 'host.json' of Durable Functions hosts,
/// but settings there can be configured to lookup the values in application settings.
/// Hence, the intention of this class is to help developers configure the expected application settings
/// so they match how we configure 'host.json'.
/// Application settings referred from 'host.json' cannot be hierarchical, which is why this options class
/// doesn't contain a 'SectionName' constant.
/// <list type="bullet">
/// <item>
///     <see href="https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-storage-providers#azure-storage">Azure storage provider</see>
/// </item>
/// <item>
///     <see href="https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-storage-providers#connections">Configuring the Azure storage provider</see>
/// </item>
/// <item>
///     <see href="https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-bindings?pivots=programming-language-csharp#hostjson-settings">host.json settings</see>
/// </item>
/// </list>
/// </summary>
public class ProcessManagerOptions
{
    /// <summary>
    /// Connection string for the Azure Storage that is used as the storage provider for Durable Functions.
    /// </summary>
    [Required]
    public string ProcessManagerStorageConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Name of the Task Hub used by Durable Function.
    /// </summary>
    [Required]
    public string ProcessManagerTaskHubName { get; set; } = string.Empty;
}
