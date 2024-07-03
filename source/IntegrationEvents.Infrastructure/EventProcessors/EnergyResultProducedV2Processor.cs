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
using Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Factories;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Microsoft.Extensions.Logging;
using EventId = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.EventId;

namespace Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.EventProcessors;

public sealed class EnergyResultProducedV2Processor : IIntegrationEventProcessor
{
    private readonly EnergyResultMessageResultFactory _energyResultMessageResultFactory;
    private readonly IOutgoingMessagesClient _outgoingMessagesClient;
    private readonly ILogger<EnergyResultProducedV2Processor> _logger;
    private readonly IFeatureFlagManager _featureManager;

    public EnergyResultProducedV2Processor(
        EnergyResultMessageResultFactory energyResultMessageResultFactory,
        IOutgoingMessagesClient outgoingMessagesClient,
        ILogger<EnergyResultProducedV2Processor> logger,
        IFeatureFlagManager featureManager)
    {
        _energyResultMessageResultFactory = energyResultMessageResultFactory;
        _outgoingMessagesClient = outgoingMessagesClient;
        _logger = logger;
        _featureManager = featureManager;
    }

    public string EventTypeToHandle => EnergyResultProducedV2.EventName;

    public async Task ProcessAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        if (!await _featureManager.UseEnergyResultProducedAsync().ConfigureAwait(false))
        {
            return;
        }

        var energyResultProducedV2 = (EnergyResultProducedV2)integrationEvent.Message;

        var isHandledByCalculationCompletedEvent = await _featureManager.UseCalculationCompletedEventAsync()
            .ConfigureAwait(false);

        if (isHandledByCalculationCompletedEvent)
            return;

        if (!EnergyResultProducedProcessorExtensions.SupportedTimeSeriesTypes().Contains(energyResultProducedV2.TimeSeriesType))
        {
            _logger.LogInformation(
                "TimeSeriesType {TimeSeriesType} is not supported",
                energyResultProducedV2.TimeSeriesType);
            return;
        }

        if (energyResultProducedV2 is
            {
                CalculationType: EnergyResultProducedV2.Types.CalculationType.WholesaleFixing,
                AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea: not null
            })
        {
            _logger.LogInformation(
                "Energy Result to a Balance Responsible per Energy Supplier should be ignored for wholesale fixing calculation type.");
            return;
        }

        var message = await _energyResultMessageResultFactory
            .CreateAsync(EventId.From(integrationEvent.EventIdentification), energyResultProducedV2, CancellationToken.None)
            .ConfigureAwait(false);

        await _outgoingMessagesClient.EnqueueAndCommitAsync(message, cancellationToken).ConfigureAwait(false);
    }
}
