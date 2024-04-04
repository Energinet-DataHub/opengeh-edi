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

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BuildingBlocks.Application.FeatureFlag;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Factories;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.EventProcessors;

public sealed class EnergyResultProducedV2Processor : IIntegrationEventProcessor
{
    private readonly EnergyResultMessageResultFactory _energyResultMessageResultFactory;
    private readonly IOutgoingMessagesClient _outgoingMessagesClient;
    private readonly ILogger<EnergyResultProducedV2Processor> _logger;
    private readonly IFeatureFlagManager _featureFlagManager;

    public EnergyResultProducedV2Processor(
        EnergyResultMessageResultFactory energyResultMessageResultFactory,
        IOutgoingMessagesClient outgoingMessagesClient,
        ILogger<EnergyResultProducedV2Processor> logger,
        IFeatureFlagManager featureFlagManager)
    {
        _energyResultMessageResultFactory = energyResultMessageResultFactory;
        _outgoingMessagesClient = outgoingMessagesClient;
        _logger = logger;
        _featureFlagManager = featureFlagManager;
    }

    public string EventTypeToHandle => EnergyResultProducedV2.EventName;

    public async Task ProcessAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        if (await _featureFlagManager.UseEnergyResultProduced.ConfigureAwait(false))
        {
            var energyResultProducedV2 = (EnergyResultProducedV2)integrationEvent.Message;

            if (!EnergyResultProducedProcessorExtensions.SupportedTimeSeriesTypes().Contains(energyResultProducedV2.TimeSeriesType))
            {
                _logger.LogInformation(
                    "TimeSeriesType {TimeSeriesType} is not supported",
                    energyResultProducedV2.TimeSeriesType);
                return;
            }

            var message = await _energyResultMessageResultFactory
                .CreateAsync(energyResultProducedV2, CancellationToken.None)
                .ConfigureAwait(false);

            await _outgoingMessagesClient.EnqueueAndCommitAsync(message, cancellationToken).ConfigureAwait(false);
        }
    }
}
