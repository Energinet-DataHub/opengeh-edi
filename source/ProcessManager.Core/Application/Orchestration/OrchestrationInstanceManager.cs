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

using Energinet.DataHub.ProcessManagement.Core.Application.Scheduling;
using Energinet.DataHub.ProcessManagement.Core.Domain.OrchestrationDescription;
using Energinet.DataHub.ProcessManagement.Core.Domain.OrchestrationInstance;
using NodaTime;

namespace Energinet.DataHub.ProcessManagement.Core.Application.Orchestration;

/// <summary>
/// An manager that allows us to provide a framework for managing orchestration instances
/// using custom domain types.
/// </summary>
internal class OrchestrationInstanceManager(
    IClock clock,
    IOrchestrationInstanceExecutor executor,
    IOrchestrationRegisterQueries orchestrationRegister,
    IOrchestrationInstanceRepository repository) :
        IStartOrchestrationInstanceCommands,
        IStartScheduledOrchestrationInstanceCommand,
        ICancelScheduledOrchestrationInstanceCommand
{
    private readonly IClock _clock = clock;
    private readonly IOrchestrationInstanceExecutor _executor = executor;
    private readonly IOrchestrationRegisterQueries _orchestrationRegister = orchestrationRegister;
    private readonly IOrchestrationInstanceRepository _repository = repository;

    /// <inheritdoc />
    public async Task<OrchestrationInstanceId> StartNewOrchestrationInstanceAsync<TParameter>(
        string name,
        int version,
        TParameter inputParameter,
        IReadOnlyCollection<int> skipStepsBySequence)
            where TParameter : class
    {
        var orchestrationDescription = await GuardMatchingOrchestrationDescriptionAsync(name, version, inputParameter, skipStepsBySequence).ConfigureAwait(false);

        var orchestrationInstance = await CreateOrchestrationInstanceAsync(inputParameter, orchestrationDescription, skipStepsBySequence).ConfigureAwait(false);
        await RequestStartOfOrchestrationInstanceAsync(orchestrationDescription, orchestrationInstance).ConfigureAwait(false);

        return orchestrationInstance.Id;
    }

    /// <inheritdoc />
    public async Task<OrchestrationInstanceId> ScheduleNewOrchestrationInstanceAsync<TParameter>(
        string name,
        int version,
        TParameter inputParameter,
        Instant runAt,
        IReadOnlyCollection<int> skipStepsBySequence)
            where TParameter : class
    {
        var orchestrationDescription = await GuardMatchingOrchestrationDescriptionAsync(name, version, inputParameter, skipStepsBySequence).ConfigureAwait(false);
        if (orchestrationDescription.CanBeScheduled == false)
            throw new InvalidOperationException("Orchestration description cannot be scheduled.");

        var orchestrationInstance = await CreateOrchestrationInstanceAsync(inputParameter, orchestrationDescription, skipStepsBySequence, runAt).ConfigureAwait(false);

        return orchestrationInstance.Id;
    }

    /// <inheritdoc />
    public async Task StartScheduledOrchestrationInstanceAsync(OrchestrationInstanceId id)
    {
        var orchestrationInstance = await _repository.GetAsync(id).ConfigureAwait(false);
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
        var orchestrationInstance = await _repository.GetAsync(id).ConfigureAwait(false);
        if (!orchestrationInstance.Lifecycle.IsPendingForScheduledStart())
            throw new InvalidOperationException("Orchestration instance cannot be canceled.");

        // Transition lifecycle
        orchestrationInstance.Lifecycle.TransitionToTerminated(_clock, OrchestrationInstanceTerminationStates.UserCanceled);
        await _repository.UnitOfWork.CommitAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Validate orchestration description is known and that paramter value is valid according to its parameter definition.
    /// </summary>
    private async Task<OrchestrationDescription> GuardMatchingOrchestrationDescriptionAsync<TParameter>(
        string name,
        int version,
        TParameter inputParameter,
        IReadOnlyCollection<int> skipStepsBySequence)
            where TParameter : class
    {
        var orchestrationDescription = await _orchestrationRegister.GetOrDefaultAsync(name, version, isEnabled: true).ConfigureAwait(false);
        if (orchestrationDescription == null)
            throw new InvalidOperationException($"No enabled orchestration description matches Name='{name}' and Version='{version}'.");

        var isValidParameterValue = await orchestrationDescription.ParameterDefinition.IsValidParameterValueAsync(inputParameter).ConfigureAwait(false);
        if (isValidParameterValue == false)
            throw new InvalidOperationException("Paramater value is not valid compared to registered parameter definition.");

        foreach (var stepSequence in skipStepsBySequence)
        {
            var stepOrDefault = orchestrationDescription.Steps.FirstOrDefault(step => step.Sequence == stepSequence);
            if (stepOrDefault == null)
                throw new InvalidOperationException($"No step description matches the sequence '{stepSequence}'.");

            if (stepOrDefault.CanBeSkipped == false)
                throw new InvalidOperationException($"Step description with sequence '{stepSequence}' cannot be skipped.");
        }

        return orchestrationDescription;
    }

    private async Task<OrchestrationInstance> CreateOrchestrationInstanceAsync<TParameter>(
        TParameter inputParameter,
        OrchestrationDescription orchestrationDescription,
        IReadOnlyCollection<int> skipStepsBySequence,
        Instant? runAt = default)
            where TParameter : class
    {
        var orchestrationInstance = OrchestrationInstance.CreateFromDescription(
            orchestrationDescription,
            skipStepsBySequence,
            _clock,
            runAt);
        orchestrationInstance.ParameterValue.SetFromInstance(inputParameter);

        await _repository.AddAsync(orchestrationInstance).ConfigureAwait(false);
        await _repository.UnitOfWork.CommitAsync().ConfigureAwait(false);

        return orchestrationInstance;
    }

    private async Task RequestStartOfOrchestrationInstanceAsync(
        OrchestrationDescription orchestrationDescription,
        OrchestrationInstance orchestrationInstance)
    {
        await _executor.StartNewOrchestrationInstanceAsync(orchestrationDescription, orchestrationInstance).ConfigureAwait(false);

        orchestrationInstance.Lifecycle.TransitionToQueued(_clock);
        await _repository.UnitOfWork.CommitAsync().ConfigureAwait(false);
    }
}
