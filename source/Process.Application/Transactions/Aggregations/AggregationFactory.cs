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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations;
using Energinet.DataHub.EDI.Process.Domain.Transactions.Exceptions;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Google.Protobuf.Collections;
using NodaTime.Serialization.Protobuf;
using NodaTime.Text;
using static Energinet.DataHub.Wholesale.Contracts.IntegrationEvents.EnergyResultProducedV2.Types;
using DecimalValue = Energinet.DataHub.Wholesale.Contracts.IntegrationEvents.Common.DecimalValue;
using GridAreaDetails = Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations.GridAreaDetails;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;
using Point = Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations.Point;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;

namespace Energinet.DataHub.EDI.Process.Application.Transactions.Aggregations;

public class AggregationFactory
{
    private readonly IMasterDataClient _masterDataClient;

    public AggregationFactory(IMasterDataClient masterDataClient)
    {
        _masterDataClient = masterDataClient;
    }

    public static Aggregation Create(
        AggregatedMeasureDataProcess aggregatedMeasureDataProcess,
        AggregatedTimeSerie aggregatedTimeSerie)
    {
        if (aggregatedMeasureDataProcess == null) throw new ArgumentNullException(nameof(aggregatedMeasureDataProcess));
        if (aggregatedTimeSerie == null) throw new ArgumentNullException(nameof(aggregatedTimeSerie));

        if ((aggregatedMeasureDataProcess.MeteringPointType != null
                ? MeteringPointType.FromCode(aggregatedMeasureDataProcess.MeteringPointType).Name
                : null) != aggregatedTimeSerie.MeteringPointType)
        {
            throw new ArgumentException("AggregatedTimeSerie.MeteringPointType isn't equal to aggregatedMeasureDataProcess.MeteringPointType", nameof(aggregatedTimeSerie));
        }

        return new Aggregation(
            MapPoints(aggregatedTimeSerie.Points),
            aggregatedTimeSerie.MeteringPointType,
            aggregatedTimeSerie.UnitType,
            aggregatedTimeSerie.Resolution,
            MapPeriod(aggregatedMeasureDataProcess.StartOfPeriod, aggregatedMeasureDataProcess.EndOfPeriod),
            MapSettlementMethod(aggregatedMeasureDataProcess),
            aggregatedMeasureDataProcess.BusinessReason.Name,
            MapActorGrouping(aggregatedMeasureDataProcess),
            MapGridAreaDetails(aggregatedTimeSerie.GridAreaDetails),
            aggregatedMeasureDataProcess.BusinessTransactionId.Id,
            aggregatedMeasureDataProcess.RequestedByActorId.Value,
            MapReceiverRole(aggregatedMeasureDataProcess),
            aggregatedMeasureDataProcess.SettlementVersion?.Name);
    }

    public async Task<Aggregation> CreateAsync(
        EnergyResultProducedV2 integrationEvent,
        CancellationToken cancellationToken)
    {
        if (integrationEvent == null) throw new ArgumentNullException(nameof(integrationEvent));

        return new Aggregation(
            MapPoints(integrationEvent.TimeSeriesPoints),
            MapMeteringPointType(integrationEvent.TimeSeriesType),
            MapQuantityUnit(integrationEvent.QuantityUnit),
            MapResolution(integrationEvent.Resolution),
            MapPeriod(integrationEvent),
            MapSettlementType(integrationEvent.TimeSeriesType),
            MapCalculationType(integrationEvent.CalculationType),
            MapActorGrouping(integrationEvent),
            await GetGridAreaDetailsAsync(integrationEvent, cancellationToken).ConfigureAwait(false),
            SettlementVersion: MapSettlementVersion(integrationEvent.CalculationType));
    }

    private static Period MapPeriod(string startOfPeriod, string? endOfPeriod)
    {
        if (string.IsNullOrEmpty(endOfPeriod)) // Throw exception since our end period is nullable in our schema contract, but we validate for it in Wholesale
            throw new ArgumentException("End of period shouldn't be able to be null, since validation in Wholesale rejects the request if it isn't set", nameof(endOfPeriod));

        return new Period(InstantPattern.General.Parse(startOfPeriod).Value, InstantPattern.General.Parse(endOfPeriod).Value);
    }

    private static GridAreaDetails MapGridAreaDetails(Domain.Transactions.AggregatedMeasureData.GridAreaDetails timeSerieGridAreaDetails)
    {
        return new GridAreaDetails(timeSerieGridAreaDetails.GridAreaCode, timeSerieGridAreaDetails.OperatorNumber);
    }

    private static List<Point> MapPoints(IEnumerable<Domain.Transactions.AggregatedMeasureData.Point> points)
    {
        return points
            .Select(point => new Point(point.Position, point.Quantity, point.QuantityQuality, point.SampleTime))
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

    private static string MapCalculationType(CalculationType processType)
    {
        return processType switch
        {
            CalculationType.Aggregation => BusinessReason.PreliminaryAggregation.Name,
            CalculationType.BalanceFixing => BusinessReason.BalanceFixing.Name,
            CalculationType.WholesaleFixing => BusinessReason.WholesaleFixing.Name,
            CalculationType.FirstCorrectionSettlement => BusinessReason.Correction.Name,
            CalculationType.SecondCorrectionSettlement => BusinessReason.Correction.Name,
            CalculationType.ThirdCorrectionSettlement => BusinessReason.Correction.Name,
            CalculationType.Unspecified => throw new InvalidOperationException(
                "Process type is not specified from Wholesales"),
            _ => throw new InvalidOperationException("Unknown process type from Wholesales"),
        };
    }

    private static string MapResolution(EnergyResultProducedV2.Types.Resolution resolution)
    {
        return resolution switch
        {
            EnergyResultProducedV2.Types.Resolution.Quarter => Resolution.QuarterHourly.Name,
            EnergyResultProducedV2.Types.Resolution.Unspecified => throw new InvalidOperationException(
                "Could not map resolution type"),
            _ => throw new InvalidOperationException("Unknown resolution type"),
        };
    }

    private static string MapQuantityUnit(QuantityUnit quantityUnit)
    {
        return quantityUnit switch
        {
            QuantityUnit.Kwh => MeasurementUnit.Kwh.Name,
            QuantityUnit.Unspecified => throw new InvalidOperationException("Could not map unit type"),
            _ => throw new InvalidOperationException("Unknown unit type"),
        };
    }

    private static string MapMeteringPointType(TimeSeriesType timeSeriesType)
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

    private static ActorGrouping MapActorGrouping(EnergyResultProducedV2 integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return integrationEvent.AggregationLevelCase switch
        {
            EnergyResultProducedV2.AggregationLevelOneofCase.AggregationPerGridarea => new ActorGrouping(null, null),
            EnergyResultProducedV2.AggregationLevelOneofCase.AggregationPerBalanceresponsiblepartyPerGridarea =>
                new ActorGrouping(
                    null,
                    integrationEvent.AggregationPerBalanceresponsiblepartyPerGridarea.BalanceResponsibleId),
            EnergyResultProducedV2.AggregationLevelOneofCase.AggregationPerEnergysupplierPerGridarea =>
                new ActorGrouping(integrationEvent.AggregationPerEnergysupplierPerGridarea.EnergySupplierId, null),
            EnergyResultProducedV2.AggregationLevelOneofCase
                    .AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea =>
                new ActorGrouping(
                    integrationEvent.AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea.EnergySupplierId,
                    integrationEvent.AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea.BalanceResponsibleId),
            EnergyResultProducedV2.AggregationLevelOneofCase.None =>
                throw new InvalidOperationException("Aggregation level is not specified"),
            _ => throw new InvalidOperationException("Aggregation level is unknown"),
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

    private static Period MapPeriod(EnergyResultProducedV2 integrationEvent)
    {
        return new Period(integrationEvent.PeriodStartUtc.ToInstant(), integrationEvent.PeriodEndUtc.ToInstant());
    }

    private static IReadOnlyList<Point> MapPoints(RepeatedField<TimeSeriesPoint> timeSeriesPoints)
    {
        var points = new List<Point>();

        var pointPosition = 1;
        foreach (var point in timeSeriesPoints)
        {
            points.Add(
                new Point(
                    pointPosition,
                    Parse(point.Quantity),
                    CalculatedQuantityQualityMapper.QuantityQualityCollectionToEdiQuality(point.QuantityQualities),
                    point.Time.ToString()));
            pointPosition++;
        }

        return points.AsReadOnly();
    }

    private static string? MapSettlementVersion(CalculationType calculationType)
    {
        return calculationType switch
        {
            CalculationType.FirstCorrectionSettlement => SettlementVersion.FirstCorrection.Name,
            CalculationType.SecondCorrectionSettlement => SettlementVersion.SecondCorrection.Name,
            CalculationType.ThirdCorrectionSettlement => SettlementVersion.ThirdCorrection.Name,
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

    private async Task<GridAreaDetails> GetGridAreaDetailsAsync(
        EnergyResultProducedV2 integrationEvent,
        CancellationToken cancellationToken)
    {
        var gridAreaCode = integrationEvent.AggregationLevelCase switch
        {
            EnergyResultProducedV2.AggregationLevelOneofCase.AggregationPerGridarea => integrationEvent
                .AggregationPerGridarea.GridAreaCode,
            EnergyResultProducedV2.AggregationLevelOneofCase.AggregationPerBalanceresponsiblepartyPerGridarea =>
                integrationEvent.AggregationPerBalanceresponsiblepartyPerGridarea.GridAreaCode,
            EnergyResultProducedV2.AggregationLevelOneofCase.AggregationPerEnergysupplierPerGridarea => integrationEvent
                .AggregationPerEnergysupplierPerGridarea.GridAreaCode,
            EnergyResultProducedV2.AggregationLevelOneofCase
                .AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea => integrationEvent
                .AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea.GridAreaCode,
            EnergyResultProducedV2.AggregationLevelOneofCase.None => throw new InvalidOperationException(
                "Aggregation level was not specified"),
            _ => throw new InvalidOperationException("Unknown aggregation level"),
        };

        var gridOperatorNumber = await _masterDataClient
            .GetGridOwnerForGridAreaCodeAsync(gridAreaCode, cancellationToken)
            .ConfigureAwait(false);

        return new GridAreaDetails(gridAreaCode, gridOperatorNumber.Value);
    }
}
