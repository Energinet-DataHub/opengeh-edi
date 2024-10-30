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
using Energinet.DataHub.ProcessManagement.Core.Domain.OrchestrationDescription;
using Energinet.DataHub.ProcessManagement.Core.Domain.OrchestrationInstance;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using NodaTime;

namespace Energinet.DataHub.ProcessManagement.Core.Infrastructure.Orchestration;

/// <summary>
/// An encapsulation of <see cref="IDurableClient"/> that allows us to
/// provide a "framework" for managing Durable Functions orchestration instances using custom domain types.
/// </summary>
public class OrchestrationInstanceManager : IOrchestrationInstanceManager
{
    private readonly IClock _clock;
    private readonly IDurableClient _durableClient;
    private readonly IOrchestrationRegisterQueries _orchestrationRegister;
    private readonly IOrchestrationInstanceRepository _orchestrationInstanceRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Construct manager.
    /// </summary>
    /// <param name="clock"></param>
    /// <param name="durableClient">Must be a Durable Task Client that is connected to
    /// the same Task Hub as the Durable Functions host containing orchestrations.</param>
    /// <param name="orchestrationRegister"></param>
    /// <param name="orchestrationInstanceRepository"></param>
    /// <param name="unitOfWork"></param>
    public OrchestrationInstanceManager(
        IClock clock,
        IDurableClient durableClient,
        IOrchestrationRegisterQueries orchestrationRegister,
        IOrchestrationInstanceRepository orchestrationInstanceRepository,
        IUnitOfWork unitOfWork)
    {
        _clock = clock;
        _durableClient = durableClient;
        _orchestrationRegister = orchestrationRegister;
        _orchestrationInstanceRepository = orchestrationInstanceRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<OrchestrationInstanceId> StartNewOrchestrationInstanceAsync<TParameter>(string name, int version, TParameter parameter)
        where TParameter : class
    {
        var orchestrationDescription = await GuardMatchingOrchestrationDescriptionAsync(name, version, parameter).ConfigureAwait(false);

        var orchestrationInstance = await CreateOrchestrationInstanceAsync(parameter, orchestrationDescription).ConfigureAwait(false);
        await RequestStartOfOrchestrationInstanceAsync(orchestrationDescription, orchestrationInstance).ConfigureAwait(false);

        return orchestrationInstance.Id;
    }

    /// <inheritdoc />
    public async Task<OrchestrationInstanceId> ScheduleNewOrchestrationInstanceAsync<TParameter>(
        string name,
        int version,
        TParameter parameter,
        Instant runAt)
        where TParameter : class
    {
        var orchestrationDescription = await GuardMatchingOrchestrationDescriptionAsync(name, version, parameter).ConfigureAwait(false);
        if (orchestrationDescription.CanBeScheduled == false)
            throw new InvalidOperationException("Orchestration description cannot be scheduled.");

        var orchestrationInstance = await CreateScheduledOrchestrationInstanceAsync(parameter, runAt, orchestrationDescription).ConfigureAwait(false);

        return orchestrationInstance.Id;
    }

    /// <inheritdoc />
    public async Task StartScheduledOrchestrationInstanceAsync(OrchestrationInstanceId id)
    {
        var orchestrationInstance = await _orchestrationInstanceRepository.GetAsync(id).ConfigureAwait(false);
        if (!orchestrationInstance.Lifecycle.IsPendingForScheduledStart())
            throw new InvalidOperationException("Orchestration instance cannot be started.");

        var orchestrationDescription = await _orchestrationRegister.GetAsync(orchestrationInstance.OrchestrationDescriptionId).ConfigureAwait(false);
        if (!orchestrationDescription.IsEnabled)
            throw new InvalidOperationException("Orchestration instance is based on a disabled orchestration definition.");

        await RequestStartOfOrchestrationInstanceAsync(orchestrationDescription, orchestrationInstance).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CancelScheduledOrchestrationInstanceAsync(OrchestrationInstanceId id)
    {
        var orchestrationInstance = await _orchestrationInstanceRepository.GetAsync(id).ConfigureAwait(false);
        if (!orchestrationInstance.Lifecycle.IsPendingForScheduledStart())
            throw new InvalidOperationException("Orchestration instance cannot be canceled.");

        // Transition lifecycle
        orchestrationInstance.Lifecycle.TransitionToTerminated(_clock, OrchestrationInstanceTerminationStates.UserCanceled);
        await _unitOfWork.CommitAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Validate orchestration description is known and that paramter value is valid according to its parameter definition.
    /// </summary>
    private async Task<OrchestrationDescription> GuardMatchingOrchestrationDescriptionAsync<TParameter>(
        string name,
        int version,
        TParameter parameter)
        where TParameter : class
    {
        var orchestrationDescription = await _orchestrationRegister.GetOrDefaultAsync(name, version, isEnabled: true).ConfigureAwait(false);
        if (orchestrationDescription == null)
        {
            throw new InvalidOperationException($"No enabled orchestration description matches Name='{name}' and Version='{version}'.");
        }

        var isValidParameterValue = await orchestrationDescription.ParameterDefinition.IsValidParameterValueAsync(parameter).ConfigureAwait(false);
        return isValidParameterValue == false
            ? throw new InvalidOperationException("Paramater value is not valid compared to registered parameter definition.")
            : orchestrationDescription;
    }

    private async Task<OrchestrationInstance> CreateOrchestrationInstanceAsync<TParameter>(
        TParameter parameter,
        OrchestrationDescription orchestrationDescription)
        where TParameter : class
    {
        var orchestrationInstance = new OrchestrationInstance(
            orchestrationDescription.Id,
            _clock);
        orchestrationInstance.ParameterValue.SetFromInstance(parameter);

        await _orchestrationInstanceRepository.AddAsync(orchestrationInstance).ConfigureAwait(false);
        await _unitOfWork.CommitAsync().ConfigureAwait(false);

        return orchestrationInstance;
    }

    private async Task<OrchestrationInstance> CreateScheduledOrchestrationInstanceAsync<TParameter>(
        TParameter parameter,
        Instant runAt,
        OrchestrationDescription orchestrationDescription)
        where TParameter : class
    {
        var orchestrationInstance = new OrchestrationInstance(
            orchestrationDescription.Id,
            _clock,
            runAt);
        orchestrationInstance.ParameterValue.SetFromInstance(parameter);

        await _orchestrationInstanceRepository.AddAsync(orchestrationInstance).ConfigureAwait(false);
        await _unitOfWork.CommitAsync().ConfigureAwait(false);

        return orchestrationInstance;
    }

    private async Task RequestStartOfOrchestrationInstanceAsync(OrchestrationDescription orchestrationDescription, OrchestrationInstance orchestrationInstance)
    {
        await _durableClient
            .StartNewAsync(
                orchestratorFunctionName: orchestrationDescription.FunctionName,
                orchestrationInstance.Id.Value.ToString(),
                input: orchestrationInstance.ParameterValue.SerializedParameterValue)
            .ConfigureAwait(false);

        orchestrationInstance.Lifecycle.TransitionToStartRequested(_clock);
        await _unitOfWork.CommitAsync().ConfigureAwait(false);
    }
}
