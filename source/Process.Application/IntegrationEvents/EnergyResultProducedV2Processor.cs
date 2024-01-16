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
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents.IntegrationEventMappers;
using Energinet.DataHub.EDI.Process.Application.Transactions.Aggregations;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.Process.Application.IntegrationEvents;

public class EnergyResultProducedV2Processor : IIntegrationEventProcessor
{
    private readonly AggregationFactory _aggregationFactory;
    private readonly IMediator _mediator;
    private readonly ILogger<EnergyResultProducedV2Processor> _logger;

    public EnergyResultProducedV2Processor(
        AggregationFactory aggregationFactory,
        IMediator mediator,
        ILogger<EnergyResultProducedV2Processor> logger)
    {
        _aggregationFactory = aggregationFactory;
        _mediator = mediator;
        _logger = logger;
    }

    public string EventTypeToHandle => EnergyResultProducedV2.EventName;

    public async Task ProcessAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        if (integrationEvent == null)
            throw new ArgumentNullException(nameof(integrationEvent));

        var energyResultProducedV2 = (EnergyResultProducedV2)integrationEvent.Message;

        if (!EnergyResultProducedProcessorExtensions.SupportedTimeSeriesTypes().Contains(energyResultProducedV2.TimeSeriesType))
        {
            _logger.LogInformation(
                "TimeSeriesType {TimeSeriesType} is not supported",
                energyResultProducedV2.TimeSeriesType);
            return;
        }

        var aggregation = await _aggregationFactory
            .CreateAsync(energyResultProducedV2, CancellationToken.None)
            .ConfigureAwait(false);

        var forwardAggregationResult = new ForwardAggregationResult(aggregation);

        await _mediator.Send(forwardAggregationResult, cancellationToken).ConfigureAwait(false);
    }
}
