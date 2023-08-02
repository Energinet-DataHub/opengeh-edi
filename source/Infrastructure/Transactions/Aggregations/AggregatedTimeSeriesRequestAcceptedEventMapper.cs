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
using System.Threading.Tasks;
using Application.Transactions.Aggregations;
using Domain.OutgoingMessages;
using Domain.Transactions;
using Domain.Transactions.AggregatedMeasureData;
using Domain.Transactions.Aggregations;
using Energinet.DataHub.Edi.Responses;
using Google.Protobuf.Collections;
using Infrastructure.InboxEvents;
using MediatR;
using Serie = Energinet.DataHub.Edi.Responses.Serie;

namespace Infrastructure.Transactions.Aggregations;

public class AggregatedTimeSeriesRequestAcceptedEventMapper : IInboxEventMapper
{
    private readonly IAggregatedMeasureDataProcessRepository _aggregatedMeasureDataProcessRepository;
    private readonly IGridAreaLookup _gridAreaLookup;

    public AggregatedTimeSeriesRequestAcceptedEventMapper(
        IGridAreaLookup gridAreaLookup,
        IAggregatedMeasureDataProcessRepository aggregatedMeasureDataProcessRepository)
    {
        _aggregatedMeasureDataProcessRepository = aggregatedMeasureDataProcessRepository;
        _gridAreaLookup = gridAreaLookup;
    }

    public async Task<IReadOnlyList<INotification>> MapFromAsync(string payload, Guid referenceId)
    {
        var inboxEvent =
            AggregatedTimeSeriesRequestAccepted.Parser.ParseJson(payload);

        var process = _aggregatedMeasureDataProcessRepository.GetById(ProcessId.Create(referenceId));
        ArgumentNullException.ThrowIfNull(process);

        var aggregations = new List<Aggregation>();
/*
        foreach (var serie in inboxEvent.Series)
        {
            aggregations.Add(new Aggregation(
                MapPoints(serie.TimeSeriesPoints),
            //     MapMeteringgPointType(serie.ti),
            //     MapUnitType(serie),
            //     MapResolution(serie),
            //    MapPeriod(serie),
                MapSettlementMethod(process),
                MapProcessType(serie),
                MapActorGrouping(process),
                await MapGridAreaDetailsAsync(serie).ConfigureAwait(false)));
        }
        */
        var hej = await MapGridAreaDetailsAsync(inboxEvent.Series.First()).ConfigureAwait(false);
        return new List<INotification>();
    }

    public bool CanHandle(string eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        return eventType.Equals(nameof(AggregatedTimeSeriesRequestAcceptedEventMapper), StringComparison.OrdinalIgnoreCase);
    }

    public string ToJson(byte[] payload)
    {
        var inboxEvent = AggregatedTimeSeriesRequestAccepted.Parser.ParseFrom(payload);
        return inboxEvent.ToString();
    }

    private static IReadOnlyList<Point> MapPoints(RepeatedField<TimeSeriesPoint> timeSeriesPoints)
    {
        var points = new List<Point>();

        var pointPosition = 1;
        foreach (var point in timeSeriesPoints)
        {
            points.Add(new Point(pointPosition, Parse(point.Quantity), MapQuality(point.QuantityQuality), point.Time.ToString()));
            pointPosition++;
        }

        return points.AsReadOnly();
    }

    // private static string MapMeteringPointType(CalculationResultCompleted integrationEvent)
    // {
    //     return integrationEvent.TimeSeriesType switch
    //     {
    //         TimeSeriesType.Production => MeteringPointType.Production.Name,
    //         TimeSeriesType.FlexConsumption => MeteringPointType.Consumption.Name,
    //         TimeSeriesType.NonProfiledConsumption => MeteringPointType.Consumption.Name,
    //         TimeSeriesType.NetExchangePerGa => MeteringPointType.Exchange.Name,
    //         TimeSeriesType.NetExchangePerNeighboringGa => MeteringPointType.Exchange.Name,
    //         TimeSeriesType.TotalConsumption => MeteringPointType.Consumption.Name,
    //         TimeSeriesType.Unspecified => throw new InvalidOperationException("Unknown metering point type"),
    //         _ => throw new InvalidOperationException("Could not determine metering point type"),
    //     };
    // }
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
            //TODO: Do we support production? Which do not have a settlement type.
        }

        return settlementTypeName;
    }

    // private static Period MapPeriod(Serie serie)
    // {
    //     return new Period(serie.PeriodStartUtc.ToInstant(), serie.PeriodEndUtc.ToInstant());
    // }
//
    // private static string MapResolution(Serie serie)
    // {
    //     return serie.TimeSeriesPoints.First().R switch
    //     {
    //         Resolution.Quarter => Domain.Transactions.Aggregations.Resolution.QuarterHourly.Name,
    //         Resolution.Unspecified => throw new InvalidOperationException("Could not map resolution type"),
    //         _ => throw new InvalidOperationException("Unknown resolution type"),
    //     };
    // }

    // private static string MapUnitType(Serie serie)
    // {
    //     return serie.QuantityUnit switch
    //     {
    //         QuantityUnit.Kwh => MeasurementUnit.Kwh.Name,
    //         QuantityUnit.Unspecified => throw new InvalidOperationException("Could not map unit type"),
    //         _ => throw new InvalidOperationException("Unknown unit type"),
    //     };
    // }
    //
    // private static string MapMeteringPointType(Serie serie)
    // {
    //     return serie.TimeSeriesType switch
    //     {
    //         TimeSeriesType.Production => MeteringPointType.Production.Name,
    //         TimeSeriesType.FlexConsumption => MeteringPointType.Consumption.Name,
    //         TimeSeriesType.NonProfiledConsumption => MeteringPointType.Consumption.Name,
    //         TimeSeriesType.NetExchangePerGa => MeteringPointType.Exchange.Name,
    //         TimeSeriesType.NetExchangePerNeighboringGa => MeteringPointType.Exchange.Name,
    //         TimeSeriesType.TotalConsumption => MeteringPointType.Consumption.Name,
    //         TimeSeriesType.Unspecified => throw new InvalidOperationException("Unknown metering point type"),
    //         _ => throw new InvalidOperationException("Could not determine metering point type"),
    //     };
    // }
    private static string MapProcessType(Serie serie)
    {
        return BusinessReason.PreliminaryAggregation.Name;
        // TODO: Is it possible to request BalanceFixing?
        /*return serie.ProcessType switch
        {
            Energinet.DataHub.Wholesale.Contracts.Events.ProcessType.Aggregation => BusinessReason.PreliminaryAggregation.Name,
            Energinet.DataHub.Wholesale.Contracts.Events.ProcessType.BalanceFixing => BusinessReason.BalanceFixing.Name,
            Energinet.DataHub.Wholesale.Contracts.Events.ProcessType.Unspecified => throw new InvalidOperationException("Process type is not specified from Wholesales"),
            _ => throw new InvalidOperationException("Unknown process type from Wholesales"),
        };*/
    }

    private static string MapQuality(QuantityQuality quality)
    {
        return quality switch
        {
            QuantityQuality.Incomplete => Quality.Incomplete.Name,
            QuantityQuality.Measured => Quality.Measured.Name,
            QuantityQuality.Missing => Quality.Missing.Name,
            QuantityQuality.Estimated => Quality.Estimated.Name,
            QuantityQuality.Unspecified => throw new InvalidOperationException("Quality is not specified"),
            _ => throw new InvalidOperationException("Unknown quality type"),
        };
    }

    private static decimal? Parse(DecimalValue? input)
    {
        if (input is null)
        {
            return null;
        }

        const decimal nanoFactor = 1_000_000_000;
        return input.Units + (input.Nanos / nanoFactor);
    }

    private async Task<GridAreaDetails> MapGridAreaDetailsAsync(Serie serie)
    {
        var gridOperatorNumber = await _gridAreaLookup.GetGridOperatorForAsync(serie.GridArea).ConfigureAwait(false);

        return new GridAreaDetails(serie.GridArea, gridOperatorNumber.Value);
    }
}
