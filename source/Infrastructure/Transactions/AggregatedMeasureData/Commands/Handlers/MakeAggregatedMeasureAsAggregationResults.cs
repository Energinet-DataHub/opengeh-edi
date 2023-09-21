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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Transactions.Aggregations;
using Domain.Actors;
using Domain.Transactions;
using Domain.Transactions.AggregatedMeasureData;
using Domain.Transactions.Aggregations;
using Domain.Transactions.Exceptions;
using Infrastructure.Configuration.Serialization;
using Infrastructure.OutgoingMessages.Common;
using MediatR;
using GridAreaDetails = Domain.Transactions.Aggregations.GridAreaDetails;
using Period = Domain.Transactions.Aggregations.Period;
using Point = Domain.Transactions.Aggregations.Point;

namespace Infrastructure.Transactions.AggregatedMeasureData.Commands.Handlers;

[Obsolete("This can be delete when all CreateAggregatedMeasureAggregationResults commands has been processed.")]
public class MakeAggregatedMeasureAsAggregationResults : IRequestHandler<CreateAggregatedMeasureAggregationResults, Unit>
{
    private readonly IAggregatedMeasureDataProcessRepository _aggregatedMeasureDataProcessRepository;
    private readonly ISerializer _serializer;
    private readonly IMediator _mediator;

    public MakeAggregatedMeasureAsAggregationResults(
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
            .GetAsync(ProcessId.Create(request.ProcessId), cancellationToken).ConfigureAwait(false);

        var responseData = _serializer.Deserialize<IList<AggregatedTimeSerie>>(process.ResponseData ?? string.Empty); //If type or its props is changed, serializer.Deserialize could throw an exception

        foreach (var timeSerie in responseData)
        {
            var notification = new AggregationResultAvailable(
                new Aggregation(
                    MapPoints(timeSerie.Points),
                    timeSerie.MeteringPointType,
                    timeSerie.UnitType,
                    timeSerie.Resolution,
                    MapPeriod(timeSerie.Period),
                    MapSettlementMethod(process),
                    MapBusinessReason(process),
                    MapActorGrouping(process),
                    MapGridAreaDetails(timeSerie.GridAreaDetails),
                    process.BusinessTransactionId.Id,
                    process.RequestedByActorId.Value,
                    MapReceiverRole(process),
                    MapSettlementVersion(timeSerie.SettlementVersion)));

            await _mediator.Publish(
                    notification,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return Unit.Value;
    }

    private static GridAreaDetails MapGridAreaDetails(Domain.Transactions.AggregatedMeasureData.GridAreaDetails timeSerieGridAreaDetails)
    {
        return new GridAreaDetails(timeSerieGridAreaDetails.GridAreaCode, timeSerieGridAreaDetails.OperatorNumber);
    }

    private static Period MapPeriod(Domain.Transactions.AggregatedMeasureData.Period timeSeriePeriod)
    {
        return new Period(timeSeriePeriod.Start, timeSeriePeriod.End);
    }

    private static List<Point> MapPoints(IReadOnlyList<Domain.Transactions.AggregatedMeasureData.Point> points)
    {
        return points.Select(point => new Point(point.Position, point.Quantity, point.Quality, point.SampleTime)).ToList();
    }

    private static string MapReceiverRole(AggregatedMeasureDataProcess process)
    {
        return MarketRole.FromCode(process.RequestedByActorRoleCode).Name;
    }

    private static ActorGrouping MapActorGrouping(AggregatedMeasureDataProcess process)
    {
        if (process.RequestedByActorRoleCode == MarketRole.BalanceResponsibleParty.Code)
        {
            return new ActorGrouping(null, process.BalanceResponsibleId);
        }

        if (process.RequestedByActorRoleCode == MarketRole.EnergySupplier.Code)
        {
            return new ActorGrouping(process.EnergySupplierId, null);
        }

        return new ActorGrouping(null, null);
    }

    private static string? MapSettlementVersion(string? settlementVersion)
    {
        var settlementVersionName = null as string;
        try
        {
            settlementVersionName = SettlementVersion.From(settlementVersion ?? string.Empty).Name;
        }
        catch (InvalidCastException)
        {
            // Settlement version is set to null.
        }

        return settlementVersionName;
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
