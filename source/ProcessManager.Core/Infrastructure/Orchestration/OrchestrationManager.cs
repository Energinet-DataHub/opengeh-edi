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

using Energinet.DataHub.ProcessManagement.Core.Application;
using Energinet.DataHub.ProcessManagement.Core.Domain;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using NodaTime;

namespace Energinet.DataHub.ProcessManagement.Core.Infrastructure.Orchestration;

/// <summary>
/// An encapsulation of <see cref="IDurableClient"/> that allows us to
/// provide a "framework" for managing orchestrations using custom domain types.
/// </summary>
public class OrchestrationManager : IOrchestrationManager
{
    private readonly IClock _clock;
    private readonly IDurableClient _durableClient;
    private readonly IOrchestrationRegisterQueries _orchestrationRegister;

    /// <summary>
    /// Construct manager.
    /// </summary>
    /// <param name="clock"></param>
    /// <param name="durableClient">Must be a Durable Task Client that is connected to
    /// the same Task Hub as the Durable Functions host containing orchestrations.</param>
    /// <param name="orchestrationRegister"></param>
    public OrchestrationManager(
        IClock clock,
        IDurableClient durableClient,
        IOrchestrationRegisterQueries orchestrationRegister)
    {
        _clock = clock;
        _durableClient = durableClient;
        _orchestrationRegister = orchestrationRegister;
    }

    /// <inheritdoc />
    public async Task<OrchestrationInstanceId> StartOrchestrationAsync<TParameter>(string name, int version, TParameter parameter)
        where TParameter : class
    {
        var orchestrationDescription = await _orchestrationRegister.GetOrDefaultAsync(name, version).ConfigureAwait(false);
        if (orchestrationDescription != null)
        {
            // TODO: Just do it...
        }

        // TODO: Lookup description in register and use 'function name' to start the orchestration.
        var functionName = "NotifyAggregatedMeasureDataOrchestrationV1";
        // TODO: Lookup description in register and validate input parameter type is valid.

        // TODO: Create instance based on description.
        var instance = new OrchestrationInstance(
            new OrchestrationDescriptionId(Guid.NewGuid()),
            _clock);
        instance.ParameterValue.SetFromInstance(parameter);

        var orchestrationInstanceId = await _durableClient
            .StartNewAsync(
                orchestratorFunctionName: functionName,
                input: instance.ParameterValue.SerializedParameterValue)
            .ConfigureAwait(false);

        return new OrchestrationInstanceId(Guid.Parse(orchestrationInstanceId));
    }

    /// <inheritdoc />
    public Task<OrchestrationInstanceId> ScheduleOrchestrationAsync<TParameter>(
        string name,
        int version,
        TParameter parameter,
        Instant runAt)
        where TParameter : class
    {
        return Task.FromResult(new OrchestrationInstanceId(Guid.NewGuid()));
    }

    /// <inheritdoc />
    public Task CancelScheduledOrchestrationAsync(OrchestrationInstanceId id)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<OrchestrationInstance>> GetOrchestrationInstancesAsync(string name, int? version)
    {
        return Task.FromResult((IReadOnlyCollection<OrchestrationInstance>)[]);
    }
}
