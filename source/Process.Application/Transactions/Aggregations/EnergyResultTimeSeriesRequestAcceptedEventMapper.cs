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

using System.Collections.ObjectModel;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData.Notifications;
using Energinet.DataHub.EDI.Process.Application.Transactions.Mappers;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Infrastructure.InboxEvents;
using Energinet.DataHub.Edi.Responses;
using Google.Protobuf.Collections;
using MediatR;
using NodaTime.Serialization.Protobuf;
using Resolution = Energinet.DataHub.Edi.Responses.Resolution;
using SettlementVersion = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.SettlementVersion;

namespace Energinet.DataHub.EDI.Process.Application.Transactions.Aggregations;

public class EnergyResultTimeSeriesRequestAcceptedEventMapper : IInboxEventMapper
{
    public Task<INotification> MapFromAsync(byte[] payload, EventId eventId, Guid referenceId, CancellationToken cancellationToken)
    {
        var aggregations =
            AggregatedTimeSeriesRequestAccepted.Parser.ParseFrom(payload);

        ArgumentNullException.ThrowIfNull(aggregations);

        var acceptedEnergyResultTimeSeries = new List<AcceptedEnergyResultTimeSeries>();
        foreach (var aggregation in aggregations.Series)
        {
            acceptedEnergyResultTimeSeries.Add(new AcceptedEnergyResultTimeSeries(
                MapPoints(aggregation.TimeSeriesPoints),
                MapMeteringPointType(aggregation.TimeSeriesType),
                MapSettlementMethod(aggregation.TimeSeriesType),
                MapUnitType(aggregation.QuantityUnit),
                MapResolution(aggregation.Resolution),
                aggregation.GridArea,
                aggregation.CalculationResultVersion,
                aggregation.Period.StartOfPeriod.ToInstant(),
                aggregation.Period.EndOfPeriod.ToInstant(),
                MapSettlementVersion(aggregation.SettlementVersion)));
        }

        return Task.FromResult<INotification>(new AggregatedTimeSeriesRequestWasAccepted(
            eventId,
            referenceId,
            acceptedEnergyResultTimeSeries));
    }

    public bool CanHandle(string eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        return eventType.Equals(nameof(AggregatedTimeSeriesRequestAccepted), StringComparison.OrdinalIgnoreCase);
    }

    private static MeteringPointType MapMeteringPointType(TimeSeriesType timeSeriesType)
    {
        return timeSeriesType switch
        {
            TimeSeriesType.Production => MeteringPointType.Production,
            TimeSeriesType.FlexConsumption => MeteringPointType.Consumption,
            TimeSeriesType.NonProfiledConsumption => MeteringPointType.Consumption,
            TimeSeriesType.NetExchangePerGa => MeteringPointType.Exchange,
            TimeSeriesType.TotalConsumption => MeteringPointType.Consumption,
            TimeSeriesType.Unspecified => throw new InvalidOperationException(
                $"Unknown {typeof(TimeSeriesType)}. Value: {timeSeriesType}'"),
            _ => throw new InvalidOperationException(
                $"Could not determine {typeof(MeteringPointType)} from 'timeSeriesType: {timeSeriesType}' of type: {typeof(TimeSeriesType)}"),
        };
    }

    private static SettlementMethod? MapSettlementMethod(TimeSeriesType timeSeriesType)
    {
        return timeSeriesType switch
        {
            TimeSeriesType.Production => null,
            TimeSeriesType.FlexConsumption => SettlementMethod.Flex,
            TimeSeriesType.NonProfiledConsumption => SettlementMethod.NonProfiled,
            TimeSeriesType.NetExchangePerGa => null,
            TimeSeriesType.TotalConsumption => null,
            TimeSeriesType.Unspecified => throw new InvalidOperationException(
                $"Unknown {typeof(TimeSeriesType)}. Value: {timeSeriesType}'"),
            _ => throw new InvalidOperationException(
                $"Could not determine {typeof(SettlementMethod)} from 'timeSeriesType: {timeSeriesType}' of type: {typeof(TimeSeriesType)}"),
        };
    }

    private static ReadOnlyCollection<Point> MapPoints(RepeatedField<TimeSeriesPoint> timeSeriesPoints)
    {
        var points = new List<Point>();

        var pointPosition = 1;
        foreach (var point in timeSeriesPoints.OrderBy(x => x.Time))
        {
            points.Add(new Point(
                pointPosition,
                Parse(point.Quantity),
                MapQuality(point.QuantityQualities),
                point.Time.ToString()));

            pointPosition++;
        }

        return points.AsReadOnly();
    }

    private static BuildingBlocks.Domain.Models.Resolution MapResolution(Resolution resolution)
    {
        return resolution switch
        {
            Resolution.Pt15M => BuildingBlocks.Domain.Models.Resolution.QuarterHourly,
            Resolution.Pt1H => BuildingBlocks.Domain.Models.Resolution.Hourly,
            Resolution.Unspecified => throw new InvalidOperationException("Could not map resolution type"),
            _ => throw new InvalidOperationException("Unknown resolution type"),
        };
    }

    private static MeasurementUnit MapUnitType(QuantityUnit quantityUnit)
    {
        return quantityUnit switch
        {
            QuantityUnit.Kwh => MeasurementUnit.KilowattHour,
            QuantityUnit.Unspecified => throw new InvalidOperationException("Could not map unit type"),
            _ => throw new InvalidOperationException("Unknown unit type"),
        };
    }

    private static CalculatedQuantityQuality MapQuality(ICollection<QuantityQuality> quantityQualities)
    {
        return CalculatedQuantityQualityMapper.MapForEnergyResults(quantityQualities);
    }

    private static SettlementVersion? MapSettlementVersion(Edi.Responses.SettlementVersion aggregationSettlementVersion)
    {
        return aggregationSettlementVersion switch
        {
            Edi.Responses.SettlementVersion.FirstCorrection => SettlementVersion.FirstCorrection,
            Edi.Responses.SettlementVersion.SecondCorrection => SettlementVersion.SecondCorrection,
            Edi.Responses.SettlementVersion.ThirdCorrection => SettlementVersion.ThirdCorrection,
            Edi.Responses.SettlementVersion.Unspecified => null,
            _ => throw new InvalidOperationException("Unknown settlement version"),
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
