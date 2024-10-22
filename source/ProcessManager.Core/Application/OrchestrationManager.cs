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

using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace Energinet.DataHub.ProcessManagement.Core.Application;

/// <summary>
/// An encapsulation of <see cref="IDurableClient"/> that allows us to
/// provide an abstraction using custom domain types to provide a "framework".
/// </summary>
/// <param name="durableClient">Must be a Durable Task Client that is connected to the same Task Hub as the Durable Functions host.</param>
public class OrchestrationManager(
    IDurableClient durableClient)
{
    private readonly IDurableClient _durableClient = durableClient;

    /// <summary>
    /// Start a new instance of an orchestration.
    /// </summary>
    public async Task StartOrchestrationAsync()
    {
        var orchestrationInstanceId = await _durableClient
            .StartNewAsync(
                "NotifyAggregatedMeasureDataOrchestration")
            .ConfigureAwait(false);
    }
}
