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
using System.Threading.Tasks;
using Application.Transactions.Aggregations;
using Domain.Transactions.Aggregations;
using Energinet.DataHub.Edi.Responses;
using Infrastructure.InboxEvents;
using MediatR;

namespace Infrastructure.Transactions.Aggregations;

public class AggregatedTimeSeriesRequestAcceptedEventMapper : IInboxEventMapper
{
    private readonly IGridAreaLookup _gridAreaLookup;

    public AggregatedTimeSeriesRequestAcceptedEventMapper(IGridAreaLookup gridAreaLookup)
    {
        _gridAreaLookup = gridAreaLookup;
    }

    public Task<INotification> MapFromAsync(string payload, Guid referenceId)
    {
        var inboxEvent =
            AggregatedTimeSeriesRequestAccepted.Parser.ParseJson(payload);
        var aggregations = new List<Aggregation>();

        foreach (var serie in inboxEvent.Series)
        {
            // aggregations.Add(new Aggregation(
            //     MapPoints(serie.TimeSeriesPoints),
            //     MapMeteringgPointType(serie.ti),
            //     MapUnitType(serie),
            //     MapResolution(serie),
            //     MapPeriod(serie),
            //     MapSettlementMethod(serie),
            //     MapProcessType(serie),
            //     MapActorGrouping(serie),
            //     await MapGridAreaDetailsAsync(serie).ConfigureAwait(false)));
        }

        return Task.FromResult<INotification>(new AggregationResultAvailable(null!));
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

    // private static IReadOnlyList<Point> MapPoints(RepeatedField<TimeSeriesPoint> timeSeriesPoints)
    // {
    //     var points = new List<Point>();
    //
    //     var pointPosition = 1;
    //     foreach (var point in timeSeriesPoints)
    //     {
    //         points.Add(new Point(pointPosition, Parse(point.Quantity), MapQuality(point.QuantityQuality), point.Time.ToString()));
    //         pointPosition++;
    //     }
    //
    //     return points.AsReadOnly();
    // }
    //
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
    //
    // private static ActorGrouping MapActorGrouping(Serie serie)
    // {
    //     return serie.AggregationLevelCase switch
    //     {
    //         CalculationResultCompleted.AggregationLevelOneofCase.AggregationPerGridarea => new ActorGrouping(null, null),
    //         CalculationResultCompleted.AggregationLevelOneofCase.AggregationPerBalanceresponsiblepartyPerGridarea => new ActorGrouping(null, serie.AggregationPerBalanceresponsiblepartyPerGridarea.BalanceResponsiblePartyGlnOrEic),
    //         CalculationResultCompleted.AggregationLevelOneofCase.AggregationPerEnergysupplierPerGridarea => new ActorGrouping(serie.AggregationPerEnergysupplierPerGridarea.EnergySupplierGlnOrEic, null),
    //         CalculationResultCompleted.AggregationLevelOneofCase.AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea => new ActorGrouping(serie.AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea.EnergySupplierGlnOrEic, serie.AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea.BalanceResponsiblePartyGlnOrEic),
    //         CalculationResultCompleted.AggregationLevelOneofCase.None => throw new InvalidOperationException("Aggregation level is not specified"),
    //         _ => throw new InvalidOperationException("Aggregation level is unknown"),
    //     };
    // }
    //
    // private static string? MapSettlementMethod(Serie serie)
    // {
    //     return serie.TimeSeriesType switch
    //     {
    //         TimeSeriesType.Production => null,
    //         TimeSeriesType.FlexConsumption => SettlementType.Flex.Name,
    //         TimeSeriesType.NonProfiledConsumption => SettlementType.NonProfiled.Name,
    //         _ => null,
    //     };
    // }
    //
    // private static Period MapPeriod(Serie serie)
    // {
    //     return new Period(serie.PeriodStartUtc.ToInstant(), serie.PeriodEndUtc.ToInstant());
    // }
    //
    // private static string MapResolution(Serie integrationEvent)
    // {
    //     return integrationEvent.Resolution switch
    //     {
    //         Resolution.Quarter => Domain.Transactions.Aggregations.Resolution.QuarterHourly.Name,
    //         Resolution.Unspecified => throw new InvalidOperationException("Could not map resolution type"),
    //         _ => throw new InvalidOperationException("Unknown resolution type"),
    //     };
    // }
    //
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
    //
    // private static string MapProcessType(Serie serie)
    // {
    //     return serie.ProcessType switch
    //     {
    //         Energinet.DataHub.Wholesale.Contracts.Events.ProcessType.Aggregation => BusinessReason.PreliminaryAggregation.Name,
    //         Energinet.DataHub.Wholesale.Contracts.Events.ProcessType.BalanceFixing => BusinessReason.BalanceFixing.Name,
    //         Energinet.DataHub.Wholesale.Contracts.Events.ProcessType.Unspecified => throw new InvalidOperationException("Process type is not specified from Wholesales"),
    //         _ => throw new InvalidOperationException("Unknown process type from Wholesales"),
    //     };
    // }
    //
    // private static string MapQuality(QuantityQuality quality)
    // {
    //     return quality switch
    //     {
    //         QuantityQuality.Incomplete => Quality.Incomplete.Name,
    //         QuantityQuality.Measured => Quality.Measured.Name,
    //         QuantityQuality.Missing => Quality.Missing.Name,
    //         QuantityQuality.Estimated => Quality.Estimated.Name,
    //         QuantityQuality.Unspecified => throw new InvalidOperationException("Quality is not specified"),
    //         _ => throw new InvalidOperationException("Unknown quality type"),
    //     };
    // }
    //
    // private static decimal? Parse(DecimalValue? input)
    // {
    //     if (input is null)
    //     {
    //         return null;
    //     }
    //
    //     const decimal nanoFactor = 1_000_000_000;
    //     return input.Units + (input.Nanos / nanoFactor);
    // }
    //
    // private async Task<GridAreaDetails> MapGridAreaDetailsAsync(Serie serie)
    // {
    //     var gridAreaCode = serie.AggregationLevelCase switch
    //     {
    //         CalculationResultCompleted.AggregationLevelOneofCase.AggregationPerGridarea => serie.AggregationPerGridarea.GridAreaCode,
    //         CalculationResultCompleted.AggregationLevelOneofCase.AggregationPerBalanceresponsiblepartyPerGridarea => serie.AggregationPerBalanceresponsiblepartyPerGridarea.GridAreaCode,
    //         CalculationResultCompleted.AggregationLevelOneofCase.AggregationPerEnergysupplierPerGridarea => serie.AggregationPerEnergysupplierPerGridarea.GridAreaCode,
    //         CalculationResultCompleted.AggregationLevelOneofCase.AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea => serie.AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea.GridAreaCode,
    //         CalculationResultCompleted.AggregationLevelOneofCase.None => throw new InvalidOperationException("Aggregation level was not specified"),
    //         _ => throw new InvalidOperationException("Unknown aggregation level"),
    //     };
    //
    //     var gridOperatorNumber = await _gridAreaLookup.GetGridOperatorForAsync(gridAreaCode).ConfigureAwait(false);
    //
    //     return new GridAreaDetails(gridAreaCode, gridOperatorNumber.Value);
    // }
}
