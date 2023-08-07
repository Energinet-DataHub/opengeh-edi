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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Configuration.Commands;
using Application.Transactions.AggregatedMeasureData.Commands;
using Application.Transactions.Aggregations;
using Domain.Actors;
using Domain.Transactions;
using Domain.Transactions.AggregatedMeasureData;
using Domain.Transactions.Aggregations;
using Domain.Transactions.Exceptions;
using Infrastructure.Configuration.Serialization;
using Infrastructure.OutgoingMessages.Common;
using MediatR;

namespace Infrastructure.Transactions.AggregatedMeasureData.Handlers;

public class CreateAggregatedMeasureAggregationResultsHandler : IRequestHandler<CreateAggregatedMeasureAggregationResults, Unit>
{
    private readonly IAggregatedMeasureDataProcessRepository _aggregatedMeasureDataProcessRepository;
    private readonly ISerializer _serializer;
    private readonly IMediator _mediator;

    public CreateAggregatedMeasureAggregationResultsHandler(
        IAggregatedMeasureDataProcessRepository aggregatedMeasureDataProcessRepository,
        ISerializer serializer,
        IMediator mediator)
    {
        _aggregatedMeasureDataProcessRepository = aggregatedMeasureDataProcessRepository;
        _serializer = serializer;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(CreateAggregatedMeasureAggregationResults request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var process = await _aggregatedMeasureDataProcessRepository
                          .GetByIdAsync(ProcessId.Create(request.ProcessId), cancellationToken).ConfigureAwait(false)
                      ?? throw ProcessNotFoundException.ProcessForProcessIdNotFound(request.ProcessId);

        var responseData = _serializer.Deserialize<IList<AggregatedTimeSerie>>(process.ResponseData ?? string.Empty);

        foreach (var timeSerie in responseData)
        {
            var notification = new AggregationResultAvailable(
                new Aggregation(
                    timeSerie.Points,
                    timeSerie.MeteringPointType,
                    timeSerie.UnitType,
                    timeSerie.Resolution,
                    timeSerie.Period,
                    MapSettlementMethod(process),
                    MapBusinessReason(process),
                    MapActorGrouping(process),
                    timeSerie.GridAreaDetails,
                    process.BusinessTransactionId.Id,
                    process.RequestedByActorId.Value,
                    MapReceiverRole(process)));

            await _mediator.Publish(
                    notification,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return Unit.Value;
    }

    private static string MapReceiverRole(AggregatedMeasureDataProcess process)
    {
        return MarketRole.FromCode(process.RequestedByActorRoleCode).Name;
    }

    private static ActorGrouping MapActorGrouping(AggregatedMeasureDataProcess process)
    {
        return new ActorGrouping(process.EnergySupplierId, process.BalanceResponsibleId);
    }

    private static string? MapSettlementMethod(AggregatedMeasureDataProcess process)
    {
        var settlementTypeName = null as string;
        try
        {
            settlementTypeName = SettlementType.From(process.SettlementMethod ?? string.Empty).Name;
        }
        catch (InvalidCastException)
        {
            // Settlement type for Production is set to null.
        }

        return settlementTypeName;
    }

    private static string MapBusinessReason(AggregatedMeasureDataProcess process)
    {
        return CimCode.To(process.BusinessReason).Name;
    }
}
