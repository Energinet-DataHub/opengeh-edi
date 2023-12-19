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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents.IntegrationEventMappers;
using Energinet.DataHub.EDI.Process.Application.Transactions.Aggregations;
using Energinet.DataHub.Wholesale.Contracts.Events;
using MediatR;

namespace Energinet.DataHub.EDI.Process.Application.IntegrationEvents;

public class CalculationResultCompletedProcessor : IIntegrationEventProcessor
{
    private readonly AggregationFactory _aggregationFactory;
    private readonly IMediator _mediator;

    public CalculationResultCompletedProcessor(AggregationFactory aggregationFactory, IMediator mediator)
    {
        _aggregationFactory = aggregationFactory;
        _mediator = mediator;
    }

    public string EventTypeToHandle => CalculationResultCompleted.EventName;

    public async Task ProcessAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        if (integrationEvent == null)
            throw new ArgumentNullException(nameof(integrationEvent));

        var calculationResultCompletedIntegrationEvent = (CalculationResultCompleted)integrationEvent.Message;

        var aggregation = await _aggregationFactory
            .CreateAsync(calculationResultCompletedIntegrationEvent, CancellationToken.None)
            .ConfigureAwait(false);

        var forwardAggregationResult = new ForwardAggregationResult(aggregation);

        await _mediator.Send(forwardAggregationResult, cancellationToken).ConfigureAwait(false);
    }
}
