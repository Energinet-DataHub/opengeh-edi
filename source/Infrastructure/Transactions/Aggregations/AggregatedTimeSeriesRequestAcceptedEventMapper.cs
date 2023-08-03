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
using Domain.Actors;
using Domain.OutgoingMessages;
using Domain.Transactions;
using Domain.Transactions.AggregatedMeasureData;
using Domain.Transactions.Aggregations;
using Energinet.DataHub.Edi.Responses;
using Google.Protobuf.Collections;
using Infrastructure.InboxEvents;
using Infrastructure.OutgoingMessages.Common;
using MediatR;
using NodaTime.Serialization.Protobuf;
using Period = Energinet.DataHub.Edi.Responses.Period;
using Resolution = Energinet.DataHub.Edi.Responses.Resolution;
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

        var aggregations = new List<AggregationResultAvailable>();

        foreach (var serie in inboxEvent.Series)
        {
            aggregations.Add(new AggregationResultAvailable(
                new Aggregation(
                MapPoints(serie.TimeSeriesPoints),
                MapMeteringPointType(serie),
                MapUnitType(serie),
                MapResolution(serie.Period.Resolution),
                MapPeriod(serie.Period),
                MapSettlementMethod(process),
                MapBusinessReason(process),
                MapActorGrouping(process),
                await MapGridAreaDetailsAsync(serie).ConfigureAwait(false),
                MapOriginalTransactionIdReference(process),
                MapReceiver(process),
                MapReceiverRole(process))));
        }

        return aggregations;
    }

    public bool CanHandle(string eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        return eventType.Equals(nameof(AggregatedTimeSeriesRequestAccepted), StringComparison.OrdinalIgnoreCase);
    }

    public string ToJson(byte[] payload)
    {
        var inboxEvent = AggregatedTimeSeriesRequestAccepted.Parser.ParseFrom(
            payload);
        return inboxEvent.ToString();
    }

    private static string MapReceiver(AggregatedMeasureDataProcess process)
    {
        return process.RequestedByActorId.Value;
    }

    private static string MapReceiverRole(AggregatedMeasureDataProcess process)
    {
        return MarketRole.FromCode(process.RequestedByActorRoleCode).Name;
    }

    private static string? MapOriginalTransactionIdReference(AggregatedMeasureDataProcess process)
    {
        return process.BusinessTransactionId.Id;
    }

    private static string MapMeteringPointType(Serie serie)
    {
        return serie.TimeSeriesType switch
        {
            TimeSeriesType.Production => MeteringPointType.Production.Name,
            TimeSeriesType.FlexConsumption => MeteringPointType.Consumption.Name,
            TimeSeriesType.NonProfiledConsumption => MeteringPointType.Consumption.Name,
            TimeSeriesType.NetExchangePerGa => MeteringPointType.Exchange.Name,
            TimeSeriesType.NetExchangePerNeighboringGa => MeteringPointType.Exchange.Name,
            TimeSeriesType.TotalConsumption => MeteringPointType.Consumption.Name,
            TimeSeriesType.Unspecified => throw new InvalidOperationException("Unknown metering point type"),
            _ => throw new InvalidOperationException("Could not determine metering point type"),
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

    private static Domain.Transactions.Aggregations.Period MapPeriod(Period period)
    {
        return new Domain.Transactions.Aggregations.Period(period.StartOfPeriod.ToInstant(), period.EndOfPeriod.ToInstant());
    }

    private static string MapResolution(Resolution resolution)
    {
        return resolution switch
        {
            Resolution.Pt15M => Domain.Transactions.Aggregations.Resolution.QuarterHourly.Name,
            Resolution.Pt1H => Domain.Transactions.Aggregations.Resolution.Hourly.Name,
            Resolution.Unspecified => throw new InvalidOperationException("Could not map resolution type"),
            _ => throw new InvalidOperationException("Unknown resolution type"),
        };
    }

    private static string MapUnitType(Serie serie)
    {
        return serie.QuantityUnit switch
        {
            QuantityUnit.Kwh => MeasurementUnit.Kwh.Name,
            QuantityUnit.Unspecified => throw new InvalidOperationException("Could not map unit type"),
            _ => throw new InvalidOperationException("Unknown unit type"),
        };
    }

    private static string MapBusinessReason(AggregatedMeasureDataProcess process)
    {
        return CimCode.To(process.BusinessReason).Name;
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
