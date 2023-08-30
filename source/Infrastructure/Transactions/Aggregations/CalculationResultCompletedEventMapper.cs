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
using Domain.OutgoingMessages;
using Domain.Transactions.Aggregations;
using Energinet.DataHub.Wholesale.Contracts.Events;
using Google.Protobuf.Collections;
using Infrastructure.Configuration.IntegrationEvents;
using MediatR;
using NodaTime.Serialization.Protobuf;
using Point = Domain.Transactions.Aggregations.Point;
using Resolution = Energinet.DataHub.Wholesale.Contracts.Events.Resolution;

namespace Infrastructure.Transactions.Aggregations;

public class CalculationResultCompletedEventMapper : IIntegrationEventMapper
{
    private readonly IGridAreaLookup _gridAreaLookup;

    public CalculationResultCompletedEventMapper(IGridAreaLookup gridAreaLookup)
    {
        _gridAreaLookup = gridAreaLookup;
    }

    public async Task<INotification> MapFromAsync(string payload)
    {
        var integrationEvent =
            CalculationResultCompleted.Parser.ParseJson(payload);

        return new AggregationResultAvailable(
            new Aggregation(
                MapPoints(integrationEvent.TimeSeriesPoints),
                MapMeteringPointType(integrationEvent.TimeSeriesType),
                MapUnitType(integrationEvent.QuantityUnit),
                MapResolution(integrationEvent.Resolution),
                MapPeriod(integrationEvent),
                MapSettlementType(integrationEvent.TimeSeriesType),
                MapProcessType(integrationEvent.ProcessType),
                MapActorGrouping(integrationEvent),
                await MapGridAreaDetailsAsync(integrationEvent).ConfigureAwait(false),
                SettlementVersion: MapSettlementVersion(integrationEvent.ProcessType)));
    }

    public bool CanHandle(string eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        return eventType.Equals(CalculationResultCompleted.EventName, StringComparison.OrdinalIgnoreCase);
    }

    public string ToJson(byte[] payload)
    {
        var integrationEvent = CalculationResultCompleted.Parser.ParseFrom(payload);
        return integrationEvent.ToString();
    }

    protected static string MapProcessType(ProcessType processType)
    {
        return processType switch
        {
            ProcessType.Aggregation => BusinessReason.PreliminaryAggregation.Name,
            ProcessType.BalanceFixing => BusinessReason.BalanceFixing.Name,
            ProcessType.WholesaleFixing => BusinessReason.WholesaleFixing.Name,
            ProcessType.FirstCorrectionSettlement => BusinessReason.Correction.Name,
            ProcessType.SecondCorrectionSettlement => BusinessReason.Correction.Name,
            ProcessType.ThirdCorrectionSettlement => BusinessReason.Correction.Name,
            ProcessType.Unspecified => throw new InvalidOperationException("Process type is not specified from Wholesales"),
            _ => throw new InvalidOperationException("Unknown process type from Wholesales"),
        };
    }

    protected static string MapResolution(Resolution resolution)
    {
        return resolution switch
        {
            Resolution.Quarter => Domain.Transactions.Aggregations.Resolution.QuarterHourly.Name,
            Resolution.Unspecified => throw new InvalidOperationException("Could not map resolution type"),
            _ => throw new InvalidOperationException("Unknown resolution type"),
        };
    }

    protected static string MapUnitType(QuantityUnit quantityUnit)
    {
        return quantityUnit switch
        {
            QuantityUnit.Kwh => MeasurementUnit.Kwh.Name,
            QuantityUnit.Unspecified => throw new InvalidOperationException("Could not map unit type"),
            _ => throw new InvalidOperationException("Unknown unit type"),
        };
    }

    protected static string MapMeteringPointType(TimeSeriesType timeSeriesType)
    {
        return timeSeriesType switch
        {
            TimeSeriesType.Production => MeteringPointType.Production.Name,
            TimeSeriesType.FlexConsumption => MeteringPointType.Consumption.Name,
            TimeSeriesType.NonProfiledConsumption => MeteringPointType.Consumption.Name,
            TimeSeriesType.NetExchangePerGa => MeteringPointType.Exchange.Name,
            TimeSeriesType.NetExchangePerNeighboringGa => MeteringPointType.Exchange.Name,
            TimeSeriesType.TotalConsumption => MeteringPointType.Consumption.Name,
            TimeSeriesType.GridLoss => MeteringPointType.Exchange.Name,
            TimeSeriesType.TempProduction => MeteringPointType.Production.Name,
            TimeSeriesType.NegativeGridLoss => MeteringPointType.Exchange.Name,
            TimeSeriesType.PositiveGridLoss => MeteringPointType.Exchange.Name,
            TimeSeriesType.TempFlexConsumption => MeteringPointType.Consumption.Name,
            TimeSeriesType.Unspecified => throw new InvalidOperationException("Unknown metering point type"),
            _ => throw new InvalidOperationException("Could not determine metering point type"),
        };
    }

    protected static ActorGrouping MapActorGrouping(CalculationResultCompleted integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return integrationEvent.AggregationLevelCase switch
        {
            CalculationResultCompleted.AggregationLevelOneofCase.AggregationPerGridarea => new ActorGrouping(null, null),
            CalculationResultCompleted.AggregationLevelOneofCase.AggregationPerBalanceresponsiblepartyPerGridarea => new ActorGrouping(null, integrationEvent.AggregationPerBalanceresponsiblepartyPerGridarea.BalanceResponsiblePartyGlnOrEic),
            CalculationResultCompleted.AggregationLevelOneofCase.AggregationPerEnergysupplierPerGridarea => new ActorGrouping(integrationEvent.AggregationPerEnergysupplierPerGridarea.EnergySupplierGlnOrEic, null),
            CalculationResultCompleted.AggregationLevelOneofCase.AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea => new ActorGrouping(integrationEvent.AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea.EnergySupplierGlnOrEic, integrationEvent.AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea.BalanceResponsiblePartyGlnOrEic),
            CalculationResultCompleted.AggregationLevelOneofCase.None => throw new InvalidOperationException("Aggregation level is not specified"),
            _ => throw new InvalidOperationException("Aggregation level is unknown"),
        };
    }

    protected static string MapQuality(QuantityQuality quality)
    {
        return quality switch
        {
            QuantityQuality.Incomplete => Quality.Incomplete.Name,
            QuantityQuality.Measured => Quality.Measured.Name,
            QuantityQuality.Missing => Quality.Missing.Name,
            QuantityQuality.Estimated => Quality.Estimated.Name,
            QuantityQuality.Calculated => Quality.Calculated.Name,
            QuantityQuality.Unspecified => throw new InvalidOperationException("Quality is not specified"),
            _ => throw new InvalidOperationException("Unknown quality type"),
        };
    }

    private static string? MapSettlementType(TimeSeriesType timeSeriesType)
    {
        return timeSeriesType switch
        {
            TimeSeriesType.Production => null,
            TimeSeriesType.FlexConsumption => SettlementType.Flex.Name,
            TimeSeriesType.NonProfiledConsumption => SettlementType.NonProfiled.Name,
            _ => null,
        };
    }

    private static Period MapPeriod(CalculationResultCompleted integrationEvent)
    {
        return new Period(integrationEvent.PeriodStartUtc.ToInstant(), integrationEvent.PeriodEndUtc.ToInstant());
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

    private static string? MapSettlementVersion(ProcessType processType)
    {
        return processType switch
        {
            ProcessType.FirstCorrectionSettlement => SettlementVersion.FirstCorrection.Name,
            ProcessType.SecondCorrectionSettlement => SettlementVersion.SecondCorrection.Name,
            ProcessType.ThirdCorrectionSettlement => SettlementVersion.ThirdCorrection.Name,
            _ => null,
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

    private async Task<GridAreaDetails> MapGridAreaDetailsAsync(CalculationResultCompleted integrationEvent)
    {
        var gridAreaCode = integrationEvent.AggregationLevelCase switch
        {
            CalculationResultCompleted.AggregationLevelOneofCase.AggregationPerGridarea => integrationEvent.AggregationPerGridarea.GridAreaCode,
            CalculationResultCompleted.AggregationLevelOneofCase.AggregationPerBalanceresponsiblepartyPerGridarea => integrationEvent.AggregationPerBalanceresponsiblepartyPerGridarea.GridAreaCode,
            CalculationResultCompleted.AggregationLevelOneofCase.AggregationPerEnergysupplierPerGridarea => integrationEvent.AggregationPerEnergysupplierPerGridarea.GridAreaCode,
            CalculationResultCompleted.AggregationLevelOneofCase.AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea => integrationEvent.AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea.GridAreaCode,
            CalculationResultCompleted.AggregationLevelOneofCase.None => throw new InvalidOperationException("Aggregation level was not specified"),
            _ => throw new InvalidOperationException("Unknown aggregation level"),
        };

        var gridOperatorNumber = await _gridAreaLookup.GetGridOperatorForAsync(gridAreaCode).ConfigureAwait(false);

        return new GridAreaDetails(gridAreaCode, gridOperatorNumber.Value);
    }
}
