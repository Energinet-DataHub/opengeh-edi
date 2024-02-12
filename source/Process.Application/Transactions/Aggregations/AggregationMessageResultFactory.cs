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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.Process.Application.Transactions.Mappers;
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations.OutgoingMessage;
using Energinet.DataHub.EDI.Process.Domain.Transactions.Exceptions;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using NodaTime.Serialization.Protobuf;
using static Energinet.DataHub.Wholesale.Contracts.IntegrationEvents.EnergyResultProducedV2.Types;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace Energinet.DataHub.EDI.Process.Application.Transactions.Aggregations;

public class AggregationMessageResultFactory
{
    private readonly IMasterDataClient _masterDataClient;

    public AggregationMessageResultFactory(IMasterDataClient masterDataClient)
    {
        _masterDataClient = masterDataClient;
    }

    public async Task<AggregationResultMessage> CreateAsync(
        AggregatedMeasureDataProcess aggregatedMeasureDataProcess,
        AggregatedTimeSerie aggregatedTimeSerie,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(aggregatedMeasureDataProcess);
        ArgumentNullException.ThrowIfNull(aggregatedTimeSerie);

        var gridAreaDetails = await GetGridAreaDetailsAsync(aggregatedTimeSerie.GridAreaDetails.GridAreaCode, cancellationToken).ConfigureAwait(false);
        return AggregationResultMessage.Create(
            aggregatedMeasureDataProcess.RequestedByActorId,
            ActorRole.FromCode(aggregatedMeasureDataProcess.RequestedByActorRoleCode),
            aggregatedMeasureDataProcess.ProcessId.Id,
            gridAreaDetails.GridAreaCode,
            aggregatedTimeSerie.MeteringPointType,
            aggregatedMeasureDataProcess.SettlementMethod,
            aggregatedTimeSerie.UnitType,
            aggregatedTimeSerie.Resolution,
            aggregatedMeasureDataProcess.EnergySupplierId,
            aggregatedMeasureDataProcess.BalanceResponsibleId,
            MapPeriod(aggregatedTimeSerie),
            TimeSeriesPointsMapper.MapPoints(aggregatedTimeSerie.Points),
            aggregatedMeasureDataProcess.BusinessReason.Name,
            aggregatedTimeSerie.CalculationResultVersion,
            settlementVersion: aggregatedMeasureDataProcess.SettlementVersion?.Name);
    }

    public async Task<AggregationResultMessage> CreateAsync(
        EnergyResultProducedV2 integrationEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var gridAreaDetails = await GetGridAreaDetailsAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
        var receiverRole = GetReceiverRole(integrationEvent.AggregationLevelCase);
        var energySupplierNumber = GetEnergySupplierNumber(integrationEvent);
        var balanceResponsibleNumber = GetBalanceResponsibleNumber(integrationEvent);
        return AggregationResultMessage.Create(
            ActorNumber.Create(balanceResponsibleNumber ?? energySupplierNumber ?? gridAreaDetails.OperatorNumber),
            receiverRole,
            ProcessId.New().Id,
            gridAreaDetails.GridAreaCode,
            MapMeteringPointType(integrationEvent.TimeSeriesType),
            MapSettlementType(integrationEvent.TimeSeriesType),
            MapQuantityUnit(integrationEvent.QuantityUnit),
            ResolutionMapper.MapResolution(integrationEvent.Resolution),
            energySupplierNumber,
            balanceResponsibleNumber,
            MapPeriod(integrationEvent),
            TimeSeriesPointsMapper.MapPoints(integrationEvent.TimeSeriesPoints),
            CalculationTypeMapper.MapCalculationType(integrationEvent.CalculationType),
            integrationEvent.CalculationResultVersion,
            settlementVersion: MapSettlementVersion(integrationEvent.CalculationType));
    }

    private static ActorRole GetReceiverRole(EnergyResultProducedV2.AggregationLevelOneofCase aggregationLevelCase)
    {
        switch (aggregationLevelCase)
        {
            case EnergyResultProducedV2.AggregationLevelOneofCase.AggregationPerGridarea:
                return ActorRole.MeteredDataResponsible;
            case EnergyResultProducedV2.AggregationLevelOneofCase.AggregationPerEnergysupplierPerGridarea:
                return ActorRole.EnergySupplier;
            case EnergyResultProducedV2.AggregationLevelOneofCase.AggregationPerBalanceresponsiblepartyPerGridarea:
            case EnergyResultProducedV2.AggregationLevelOneofCase
                .AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea:
                return ActorRole.BalanceResponsibleParty;
            case EnergyResultProducedV2.AggregationLevelOneofCase.None:
                throw new InvalidOperationException("Aggregation level is not specified");
            default:
                throw new InvalidOperationException("Aggregation level is unknown");
        }
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

    private static Period MapPeriod(AggregatedTimeSerie aggregatedTimeSerie)
    {
        return new Period(aggregatedTimeSerie.StartOfPeriod, aggregatedTimeSerie.EndOfPeriod);
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

        return await GetGridAreaDetailsAsync(gridAreaCode, cancellationToken).ConfigureAwait(false);
    }

    private async Task<GridAreaDetails> GetGridAreaDetailsAsync(
        string gridAreaCode,
        CancellationToken cancellationToken)
    {
        var gridOperatorNumber = await _masterDataClient
            .GetGridOwnerForGridAreaCodeAsync(gridAreaCode, cancellationToken)
            .ConfigureAwait(false);

        return new GridAreaDetails(gridAreaCode, gridOperatorNumber.Value);
    }
}
