﻿// Copyright 2020 Energinet DataHub A/S
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Process.Application.Transactions.Aggregations;
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations.OutgoingMessage;
using MediatR;

namespace Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData.Commands.Handlers;

public class AcceptProcessWhenAcceptedAggregatedTimeSeriesIsAvailable : IRequestHandler<AcceptedAggregatedTimeSerie, Unit>
{
    private readonly IAggregatedMeasureDataProcessRepository _aggregatedMeasureDataProcessRepository;

    public AcceptProcessWhenAcceptedAggregatedTimeSeriesIsAvailable(
        IAggregatedMeasureDataProcessRepository aggregatedMeasureDataProcessRepository)
    {
        _aggregatedMeasureDataProcessRepository = aggregatedMeasureDataProcessRepository;
    }

    public async Task<Unit> Handle(AcceptedAggregatedTimeSerie request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var process = await _aggregatedMeasureDataProcessRepository
            .GetAsync(ProcessId.Create(request.ProcessId), cancellationToken).ConfigureAwait(false);

        var aggregationResultMessages = new List<AggregationResultMessage>();
        foreach (var aggregatedTimeSerie in request.AggregatedTimeSeries)
        {
            var message = AggregationMessageResultFactory
                .Create(process, aggregatedTimeSerie);
            aggregationResultMessages.Add(message);
        }

        process.IsAccepted(aggregationResultMessages);

        return Unit.Value;
    }
}
