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
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations.OutgoingMessage;
using Energinet.DataHub.EDI.Process.Domain.Transactions.Exceptions;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Google.Protobuf.Collections;
using NodaTime.Serialization.Protobuf;
using static Energinet.DataHub.Wholesale.Contracts.IntegrationEvents.EnergyResultProducedV2.Types;
using DecimalValue = Energinet.DataHub.Wholesale.Contracts.IntegrationEvents.Common.DecimalValue;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;

namespace Energinet.DataHub.EDI.Process.Application.Transactions.Aggregations;

public class AggregationResultMessageFactory
{
    private readonly IMasterDataClient _masterDataClient;

    public AggregationResultMessageFactory(IMasterDataClient masterDataClient)
    {
        _masterDataClient = masterDataClient;
    }

    public async Task<AggregationResultMessage> CreateAsync(
        EnergyResultProducedV2 integrationEvent,
        ProcessId processId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(processId);
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var gridAreaDetails = await GetGridAreaDetailsAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
        var receiverRole = GetReceiverRole(integrationEvent.AggregationLevelCase);
        var energySupplierNumber = GetEnergySupplierNumber(integrationEvent);
        var balanceResponsibleNumber = GetBalanceResponsibleNumber(integrationEvent);
        return AggregationResultMessage.Create(
            ActorNumber.Create(balanceResponsibleNumber ?? energySupplierNumber ?? gridAreaDetails.OperatorNumber),
            receiverRole,
            processId.Id,
            gridAreaDetails,
            MapMeteringPointType(integrationEvent.TimeSeriesType),
            MapSettlementType(integrationEvent.TimeSeriesType),
            MapQuantityUnit(integrationEvent.QuantityUnit),
            MapResolution(integrationEvent.Resolution),
            energySupplierNumber,
            balanceResponsibleNumber,
            MapPeriod(integrationEvent),
            MapPoints(integrationEvent.TimeSeriesPoints),
            MapCalculationType(integrationEvent.CalculationType),
            integrationEvent.CalculationResultVersion,
            settlementVersion: MapSettlementVersion(integrationEvent.CalculationType));
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

    private static ActorRole GetReceiverRole(EnergyResultProducedV2.AggregationLevelOneofCase aggregationLevelCase)
    {
        return aggregationLevelCase switch
        {
            EnergyResultProducedV2.AggregationLevelOneofCase
                .AggregationPerGridarea => ActorRole.MeteredDataResponsible,
            EnergyResultProducedV2.AggregationLevelOneofCase
                .AggregationPerEnergysupplierPerGridarea => ActorRole.EnergySupplier,
            EnergyResultProducedV2.AggregationLevelOneofCase
                .AggregationPerBalanceresponsiblepartyPerGridarea => ActorRole.BalanceResponsibleParty,
            EnergyResultProducedV2.AggregationLevelOneofCase
                    .AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea => ActorRole.BalanceResponsibleParty,
            EnergyResultProducedV2.AggregationLevelOneofCase.None =>
                throw new InvalidOperationException("Aggregation level is not specified"),
            _ => throw new InvalidOperationException("Aggregation level is unknown"),
        };
    }

    private static string? GetEnergySupplierNumber(EnergyResultProducedV2 integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return integrationEvent.AggregationLevelCase switch
        {
            EnergyResultProducedV2.AggregationLevelOneofCase.AggregationPerEnergysupplierPerGridarea =>
                integrationEvent.AggregationPerEnergysupplierPerGridarea.EnergySupplierId,
            EnergyResultProducedV2.AggregationLevelOneofCase
                    .AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea =>
                integrationEvent.AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea.EnergySupplierId,
            EnergyResultProducedV2.AggregationLevelOneofCase.None =>
                throw new InvalidOperationException("Aggregation level is not specified"),
            _ => null,
        };
    }

    private static string? GetBalanceResponsibleNumber(EnergyResultProducedV2 integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return integrationEvent.AggregationLevelCase switch
        {
            EnergyResultProducedV2.AggregationLevelOneofCase.AggregationPerBalanceresponsiblepartyPerGridarea =>
                    integrationEvent.AggregationPerBalanceresponsiblepartyPerGridarea.BalanceResponsibleId,
            EnergyResultProducedV2.AggregationLevelOneofCase
                    .AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea =>
                    integrationEvent.AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea.BalanceResponsibleId,
            EnergyResultProducedV2.AggregationLevelOneofCase.None =>
                throw new InvalidOperationException("Aggregation level is not specified"),
            _ => null,
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

    private static ReadOnlyCollection<Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations.OutgoingMessage.Point> MapPoints(RepeatedField<TimeSeriesPoint> timeSeriesPoints)
    {
        var points = new List<Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations.OutgoingMessage.Point>();

        var pointPosition = 1;
        foreach (var point in timeSeriesPoints)
        {
            points.Add(
                new Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations.OutgoingMessage.Point(
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
