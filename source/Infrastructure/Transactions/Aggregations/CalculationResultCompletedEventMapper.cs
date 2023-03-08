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
using Application.Transactions.Aggregations;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Domain.Transactions;
using Domain.Transactions.Aggregations;
using Energinet.DataHub.Wholesale.Contracts.Events;
using Google.Protobuf.Collections;
using Infrastructure.Configuration.IntegrationEvents;
using MediatR;
using NodaTime.Serialization.Protobuf;
using Point = Domain.Transactions.Aggregations.Point;
using ProcessType = Domain.OutgoingMessages.ProcessType;
using Resolution = Energinet.DataHub.Wholesale.Contracts.Events.Resolution;

namespace Infrastructure.Transactions.Aggregations;

public class CalculationResultCompletedEventMapper : IIntegrationEventMapper
{
    public INotification MapFrom(byte[] payload)
    {
        var integrationEvent =
            CalculationResultCompleted.Parser.ParseFrom(payload);
        return new AggregationResultAvailable(
            new Aggregation(
                MapPoints(integrationEvent.TimeSeriesPoints),
                MapGridArea(integrationEvent),
                MapMeteringPointType(integrationEvent),
                MapUnitType(integrationEvent),
                MapResolution(integrationEvent),
                MapPeriod(integrationEvent),
                MapSettlementMethod(integrationEvent),
                MapProcessType(integrationEvent)));
    }

    public bool CanHandle(string eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        return eventType.Equals("calculationresultcompleted", StringComparison.OrdinalIgnoreCase);
    }

    private static string? MapSettlementMethod(CalculationResultCompleted integrationEvent)
    {
        return integrationEvent.TimeSeriesType switch
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

    private static string MapResolution(CalculationResultCompleted integrationEvent)
    {
        return integrationEvent.Resolution switch
        {
            Resolution.Quarter => Domain.Transactions.Resolution.QuarterHourly.Name,
            Resolution.Unspecified => throw new InvalidOperationException("Could not map resolution type"),
            _ => throw new InvalidOperationException("Unknown resolution type"),
        };
    }

    private static string MapUnitType(CalculationResultCompleted integrationEvent)
    {
        return integrationEvent.QuantityUnit switch
        {
            QuantityUnit.Kwh => MeasurementUnit.Kwh.Name,
            QuantityUnit.Unspecified => throw new InvalidOperationException("Could not map unit type"),
            _ => throw new InvalidOperationException("Unknown unit type"),
        };
    }

    private static string MapMeteringPointType(CalculationResultCompleted integrationEvent)
    {
        return integrationEvent.TimeSeriesType switch
        {
            TimeSeriesType.Production => MeteringPointType.Production.Name,
            TimeSeriesType.FlexConsumption => MeteringPointType.Consumption.Name,
            TimeSeriesType.NonProfiledConsumption => MeteringPointType.Consumption.Name,
            TimeSeriesType.Unspecified => throw new InvalidOperationException("Unknown metering point type"),
            _ => throw new InvalidOperationException("Could not determine metering point type"),
        };
    }

    private static string MapGridArea(CalculationResultCompleted integrationEvent)
    {
        return integrationEvent.AggregationLevelCase switch
        {
            CalculationResultCompleted.AggregationLevelOneofCase.AggregationPerBalanceresponsiblepartyPerGridarea => integrationEvent.AggregationPerBalanceresponsiblepartyPerGridarea.GridAreaCode,
            CalculationResultCompleted.AggregationLevelOneofCase.AggregationPerEnergysupplierPerGridarea => integrationEvent.AggregationPerEnergysupplierPerGridarea.GridAreaCode,
            CalculationResultCompleted.AggregationLevelOneofCase.AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea => integrationEvent.AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea.GridAreaCode,
            CalculationResultCompleted.AggregationLevelOneofCase.AggregationPerGridarea => integrationEvent.AggregationPerGridarea.GridAreaCode,
            CalculationResultCompleted.AggregationLevelOneofCase.None => throw new InvalidOperationException("Could not determine grid area since aggregation level is not specified"),
            _ => throw new InvalidOperationException("Unknown aggregation level"),
        };
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

    private static string MapProcessType(CalculationResultCompleted integrationEvent)
    {
        return integrationEvent.ProcessType switch
        {
            Energinet.DataHub.Wholesale.Contracts.Events.ProcessType.BalanceFixing => ProcessType.BalanceFixing.Name,
            Energinet.DataHub.Wholesale.Contracts.Events.ProcessType.Unspecified => throw new InvalidOperationException("Process type is not specified"),
            _ => throw new InvalidOperationException("Unknown process type"),
        };
    }

    private static string MapQuality(QuantityQuality quality)
    {
        return quality switch
        {
            QuantityQuality.Incomplete => Quality.Incomplete.Name,
            QuantityQuality.Measured => Quality.Measured.Name,
            QuantityQuality.Missing => Quality.Missing.Name,
            QuantityQuality.Read => Quality.Estimated.Name,
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
}
