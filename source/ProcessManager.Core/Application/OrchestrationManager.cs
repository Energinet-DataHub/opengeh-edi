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

using Energinet.DataHub.ProcessManagement.Core.Domain;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using NodaTime;

namespace Energinet.DataHub.ProcessManagement.Core.Application;

/// <summary>
/// An encapsulation of <see cref="IDurableClient"/> that allows us to
/// provide a "framework" for managing orchestrations using custom domain types.
/// </summary>
/// <param name="durableClient">Must be a Durable Task Client that is connected to
/// the same Task Hub as the Durable Functions host containing orchestrations.</param>
public class OrchestrationManager(
    IDurableClient durableClient)
{
    private readonly IDurableClient _durableClient = durableClient;

    /// <summary>
    /// Start a new instance of an orchestration.
    /// </summary>
    public async Task StartOrchestrationAsync(string name, int version)
    {
        var orchestrationInstanceId = await _durableClient
            .StartNewAsync(
                $"{name}V{version}")
            .ConfigureAwait(false);
    }

    public OrchestrationInstanceId ScheduleOrchestration(
        string name,
        int version,
        IReadOnlyCollection<OrchestrationParameterValue>? parameters,
        Instant runAt)
    {
        return new OrchestrationInstanceId(Guid.NewGuid());
    }

    public void CancelScheduledOrchestration(
        OrchestrationInstanceId id)
    {
    }

    public IReadOnlyCollection<OrchestrationInstance> GetOrchestrations(string name, int? version)
    {
        return new List<OrchestrationInstance>();
    }

    public void UpdateOrchestration(OrchestrationInstance orchestration)
    {
    }
}
