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

using BuildingBlocks.Application.FeatureFlag;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Model;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.ContextImplementations;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.EventProcessors;

public sealed class CalculationCompletedV1Processor : IIntegrationEventProcessor
{
    private readonly ILogger<CalculationCompletedV1Processor> _logger;
    private readonly IFeatureFlagManager _featureManager;
    private readonly IDurableClientFactory _durableClientFactory;

    public CalculationCompletedV1Processor(
        ILogger<CalculationCompletedV1Processor> logger,
        IFeatureFlagManager featureManager,
        IDurableClientFactory durableClientFactory)
    {
        _logger = logger;
        _featureManager = featureManager;
        _durableClientFactory = durableClientFactory;
    }

    public string EventTypeToHandle => CalculationCompletedV1.EventName;

    public async Task ProcessAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        if (!await _featureManager.UseCalculationCompletedEventAsync().ConfigureAwait(false))
        {
            _logger.LogInformation(
                "CalculationCompletedV1 event (id: {EventIdentification}) skipped because UseCalculationCompletedEvent feature is disabled.",
                integrationEvent.EventIdentification);
            return;
        }

        var message = (CalculationCompletedV1)integrationEvent.Message;

        var isFeatureEnabledForCalculationType = await _featureManager.UseCalculationCompletedEventAsync()
            .ConfigureAwait(false);

        if (!isFeatureEnabledForCalculationType)
        {
            _logger.LogInformation(
                "CalculationCompletedV1 event (id: {EventIdentification}) skipped because UseCalculationCompletedEvent feature is disabled for calculation type (type: {CalculationType}, calculation id: {CalculationId}, instance id: {OrchestrationInstanceId}).",
                integrationEvent.EventIdentification,
                message.CalculationType,
                message.CalculationId,
                message.InstanceId);
            return;
        }

        var durableClient = _durableClientFactory.CreateClient();
        var orchestrationInput = CreateOrchestrationInput(message, integrationEvent.EventIdentification);
        var instanceId = await durableClient.StartNewAsync("EnqueueMessagesOrchestration", orchestrationInput).ConfigureAwait(false);

        _logger.LogInformation(
            "Started 'EnqueueMessagesOrchestration' (id '{OrchestrationInstanceId}', calculation id: {CalculationId}, calculation type: {CalculationType}, calculation orchestration id: {CalculationOrchestrationId}.",
            instanceId,
            message.CalculationId,
            message.CalculationType,
            message.InstanceId);
    }

    private static EnqueueMessagesOrchestrationInput CreateOrchestrationInput(CalculationCompletedV1 message, Guid eventIdentification)
    {
        return new EnqueueMessagesOrchestrationInput(
            CalculationOrchestrationId: message.InstanceId,
            CalculationId: Guid.Parse(message.CalculationId),
            EventId: eventIdentification);
    }
}
