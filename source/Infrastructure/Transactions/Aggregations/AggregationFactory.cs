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
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Domain.Transactions.Aggregations;
using Energinet.DataHub.EDI.Domain.Transactions.Exceptions;
using Energinet.DataHub.EDI.Infrastructure.OutgoingMessages.Common;
using Energinet.DataHub.Wholesale.Contracts.Events;
using Google.Protobuf.Collections;
using NodaTime.Serialization.Protobuf;
using GridAreaDetails = Energinet.DataHub.EDI.Domain.Transactions.Aggregations.GridAreaDetails;
using Period = Energinet.DataHub.EDI.Domain.Transactions.Aggregations.Period;
using Point = Energinet.DataHub.EDI.Domain.Transactions.Aggregations.Point;
using Resolution = Energinet.DataHub.Wholesale.Contracts.Events.Resolution;

namespace Energinet.DataHub.EDI.Infrastructure.Transactions.Aggregations;

public static class AggregationFactory
{
    public static Aggregation Create(CalculationResultCompleted integrationEvent)
    {
        if (integrationEvent == null) throw new ArgumentNullException(nameof(integrationEvent));

        return new Aggregation(
            MapPoints(integrationEvent.TimeSeriesPoints),
            MapMeteringPointTypeFromCalculationResult(integrationEvent.TimeSeriesType),
            MapQuantityUnitFromCalculationResult(integrationEvent.QuantityUnit),
            MapResolutionFromCalculationResult(integrationEvent.Resolution),
            MapPeriod(integrationEvent),
            MapSettlementType(integrationEvent.TimeSeriesType),
            MapProcessTypeFromCalculationResult(integrationEvent.ProcessType),
            MapActorGrouping(integrationEvent),
            MapGridAreaDetails(integrationEvent),
            SettlementVersion: MapSettlementVersion(integrationEvent.ProcessType));
    }

    public static Aggregation Create(
        AggregatedMeasureDataProcess aggregatedMeasureDataProcess,
        AggregatedTimeSerie aggregatedTimeSerie)
    {
        if (aggregatedMeasureDataProcess == null) throw new ArgumentNullException(nameof(aggregatedMeasureDataProcess));
        if (aggregatedTimeSerie == null) throw new ArgumentNullException(nameof(aggregatedTimeSerie));

        return new Aggregation(
            MapPoints(aggregatedTimeSerie.Points),
            aggregatedTimeSerie.MeteringPointType,
            aggregatedTimeSerie.UnitType,
            aggregatedTimeSerie.Resolution,
            MapPeriod(aggregatedTimeSerie.Period),
            MapSettlementMethod(aggregatedMeasureDataProcess),
            MapBusinessReason(aggregatedMeasureDataProcess),
            MapActorGrouping(aggregatedMeasureDataProcess),
            MapGridAreaDetails(aggregatedTimeSerie.GridAreaDetails),
            aggregatedMeasureDataProcess.BusinessTransactionId.Id,
            aggregatedMeasureDataProcess.RequestedByActorId.Value,
            MapReceiverRole(aggregatedMeasureDataProcess),
            MapSettlementVersion(aggregatedTimeSerie.SettlementVersion));
    }

    private static GridAreaDetails MapGridAreaDetails(
        Domain.Transactions.AggregatedMeasureData.GridAreaDetails timeSerieGridAreaDetails)
    {
        return new GridAreaDetails(timeSerieGridAreaDetails.GridAreaCode, timeSerieGridAreaDetails.OperatorNumber);
    }

    private static Period MapPeriod(Domain.Transactions.AggregatedMeasureData.Period timeSeriePeriod)
    {
        return new Period(timeSeriePeriod.Start, timeSeriePeriod.End);
    }

    private static List<Point> MapPoints(IReadOnlyList<Domain.Transactions.AggregatedMeasureData.Point> points)
    {
        return points.Select(point => new Point(point.Position, point.Quantity, point.Quality, point.SampleTime))
            .ToList();
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

    private static string MapProcessTypeFromCalculationResult(ProcessType processType)
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

    private static string MapResolutionFromCalculationResult(Resolution resolution)
    {
        return resolution switch
        {
            Resolution.Quarter => Domain.Transactions.Aggregations.Resolution.QuarterHourly.Name,
            Resolution.Unspecified => throw new InvalidOperationException("Could not map resolution type"),
            _ => throw new InvalidOperationException("Unknown resolution type"),
        };
    }

    private static string MapQuantityUnitFromCalculationResult(QuantityUnit quantityUnit)
    {
        return quantityUnit switch
        {
            QuantityUnit.Kwh => MeasurementUnit.Kwh.Name,
            QuantityUnit.Unspecified => throw new InvalidOperationException("Could not map unit type"),
            _ => throw new InvalidOperationException("Unknown unit type"),
        };
    }

    private static string MapMeteringPointTypeFromCalculationResult(TimeSeriesType timeSeriesType)
    {
        return timeSeriesType switch
        {
            TimeSeriesType.Production => MeteringPointType.Production.Name,
            TimeSeriesType.FlexConsumption => MeteringPointType.Consumption.Name,
            TimeSeriesType.NonProfiledConsumption => MeteringPointType.Consumption.Name,
            TimeSeriesType.NetExchangePerGa => MeteringPointType.Exchange.Name,
            TimeSeriesType.NetExchangePerNeighboringGa => MeteringPointType.Exchange.Name,
            TimeSeriesType.TotalConsumption => MeteringPointType.Consumption.Name,
            TimeSeriesType.GridLoss => throw new NotSupportedTimeSeriesTypeException("GridLoss is not a supported TimeSeriesType"),
            TimeSeriesType.TempProduction => throw new NotSupportedTimeSeriesTypeException("TempProduction is not a supported TimeSeriesType"),
            TimeSeriesType.NegativeGridLoss => throw new NotSupportedTimeSeriesTypeException("NegativeGridLoss is not a supported TimeSeriesType"),
            TimeSeriesType.PositiveGridLoss => throw new NotSupportedTimeSeriesTypeException("PositiveGridLoss is not a supported TimeSeriesType"),
            TimeSeriesType.TempFlexConsumption => throw new NotSupportedTimeSeriesTypeException("TempFlexConsumption is not a supported TimeSeriesType"),
            TimeSeriesType.Unspecified => throw new InvalidOperationException("Unknown metering point type"),
            _ => throw new InvalidOperationException("Could not determine metering point type"),
        };
    }

    private static ActorGrouping MapActorGrouping(CalculationResultCompleted integrationEvent)
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

    private static string MapQualityFromCalculationResult(QuantityQuality quality)
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
            points.Add(new Point(pointPosition, Parse(point.Quantity), MapQualityFromCalculationResult(point.QuantityQuality), point.Time.ToString()));
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

    private static GridAreaDetails MapGridAreaDetails(CalculationResultCompleted integrationEvent)
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

        var gridOperatorNumber = GridAreaLookup.GetGridOperatorFor(gridAreaCode);

        return new GridAreaDetails(gridAreaCode, gridOperatorNumber.Value);
    }
}
