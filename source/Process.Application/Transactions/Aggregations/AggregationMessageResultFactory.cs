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

    public static AggregationResultMessage Create(
        AggregatedMeasureDataProcess aggregatedMeasureDataProcess,
        AggregatedTimeSerie aggregatedTimeSerie)
    {
        ArgumentNullException.ThrowIfNull(aggregatedMeasureDataProcess);
        ArgumentNullException.ThrowIfNull(aggregatedTimeSerie);

        return AggregationResultMessage.Create(
            receiverNumber: aggregatedMeasureDataProcess.RequestedByActorId,
            receiverRole: ActorRole.FromCode(aggregatedMeasureDataProcess.RequestedByActorRoleCode),
            processId: aggregatedMeasureDataProcess.ProcessId.Id,
            gridAreaCode: aggregatedTimeSerie.GridAreaDetails.GridAreaCode,
            meteringPointType: aggregatedTimeSerie.MeteringPointType,
            settlementType: aggregatedMeasureDataProcess.SettlementMethod,
            measureUnitType: aggregatedTimeSerie.UnitType,
            resolution: aggregatedTimeSerie.Resolution,
            energySupplierNumber: aggregatedMeasureDataProcess.EnergySupplierId,
            balanceResponsibleNumber: aggregatedMeasureDataProcess.BalanceResponsibleId,
            period: new Period(aggregatedTimeSerie.StartOfPeriod, aggregatedTimeSerie.EndOfPeriod),
            points: TimeSeriesPointsMapper.MapPoints(aggregatedTimeSerie.Points),
            businessReasonName: aggregatedMeasureDataProcess.BusinessReason.Name,
            calculationResultVersion: aggregatedTimeSerie.CalculationResultVersion,
            settlementVersion: aggregatedMeasureDataProcess.SettlementVersion?.Name);
    }

    public async Task<AggregationResultMessage> CreateAsync(
        EnergyResultProducedV2 integrationEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var balanceResponsibleNumber = GetBalanceResponsibleNumberForAggregationLevel(integrationEvent);
        var energySupplierNumber = GetEnergySupplierNumberForAggregationLevel(integrationEvent);
        var gridAreaCode = GetGridAreaCodeForAggregationLevel(integrationEvent);

        var receiverRole = GetReceiverRoleForAggregationLevel(integrationEvent.AggregationLevelCase);
        var receiverNumber =
            balanceResponsibleNumber != null ? ActorNumber.Create(balanceResponsibleNumber)
            : energySupplierNumber != null ? ActorNumber.Create(energySupplierNumber)
            : await GetGridAreaOperatorNumberAsync(gridAreaCode, cancellationToken).ConfigureAwait(false);

        var aggregationData =
            await GetAggregationLevelDataAsync(integrationEvent, cancellationToken).ConfigureAwait(false);

        if (aggregationData.BalanceResponsibleNumber != balanceResponsibleNumber) throw new InvalidOperationException("BalanceResponsibleNumber");
        if (aggregationData.EnergySupplierNumber != energySupplierNumber) throw new InvalidOperationException("EnergySupplierNumber");
        if (aggregationData.GridAreaCode != gridAreaCode) throw new InvalidOperationException("GridAreaCode");
        if (aggregationData.ReceiverRole != receiverRole) throw new InvalidOperationException("ReceiverRole");
        if (aggregationData.ReceiverNumber != receiverNumber) throw new InvalidOperationException("ReceiverNumber");

        return AggregationResultMessage.Create(
            receiverNumber: aggregationData.ReceiverNumber,
            receiverRole: aggregationData.ReceiverRole,
            processId: ProcessId.New().Id,
            gridAreaCode: aggregationData.GridAreaCode,
            meteringPointType: MeteringPointTypeMapper.MapMeteringPointType(integrationEvent.TimeSeriesType).Name,
            settlementType: SettlementTypeMapper.MapSettlementType(integrationEvent.TimeSeriesType)?.Name,
            measureUnitType: MeasurementUnitMapper.MapQuantityUnit(integrationEvent.QuantityUnit).Name,
            resolution: ResolutionMapper.MapResolution(integrationEvent.Resolution).Name,
            energySupplierNumber: aggregationData.EnergySupplierNumber,
            balanceResponsibleNumber: aggregationData.BalanceResponsibleNumber,
            period: new Period(integrationEvent.PeriodStartUtc.ToInstant(), integrationEvent.PeriodEndUtc.ToInstant()),
            points: TimeSeriesPointsMapper.MapPoints(integrationEvent.TimeSeriesPoints),
            businessReasonName: CalculationTypeMapper.MapCalculationType(integrationEvent.CalculationType).Name,
            calculationResultVersion: integrationEvent.CalculationResultVersion,
            settlementVersion: SettlementVersionMapper.MapSettlementVersion(integrationEvent.CalculationType)?.Name);
    }

    private static ActorRole GetReceiverRoleForAggregationLevel(EnergyResultProducedV2.AggregationLevelOneofCase aggregationLevelCase)
    {
        switch (aggregationLevelCase)
        {
            case EnergyResultProducedV2.AggregationLevelOneofCase.AggregationPerGridarea:
                return ActorRole.MeteredDataResponsible;
            case EnergyResultProducedV2.AggregationLevelOneofCase.AggregationPerEnergysupplierPerGridarea:
                return ActorRole.EnergySupplier;
            case EnergyResultProducedV2.AggregationLevelOneofCase.AggregationPerBalanceresponsiblepartyPerGridarea:
            case EnergyResultProducedV2.AggregationLevelOneofCase.AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea:
                return ActorRole.BalanceResponsibleParty;
            case EnergyResultProducedV2.AggregationLevelOneofCase.None:
                throw new InvalidOperationException("Aggregation level is not specified");
            default:
                throw new InvalidOperationException("Aggregation level is unknown");
        }
    }

    private static string? GetEnergySupplierNumberForAggregationLevel(EnergyResultProducedV2 integrationEvent)
    {
        return integrationEvent.AggregationLevelCase switch
        {
            EnergyResultProducedV2.AggregationLevelOneofCase.AggregationPerEnergysupplierPerGridarea => integrationEvent.AggregationPerEnergysupplierPerGridarea.EnergySupplierId,
            EnergyResultProducedV2.AggregationLevelOneofCase.AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea => integrationEvent.AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea.EnergySupplierId,
            EnergyResultProducedV2.AggregationLevelOneofCase.None => throw new InvalidOperationException("Aggregation level is not specified"),
            _ => null,
        };
    }

    private static string? GetBalanceResponsibleNumberForAggregationLevel(EnergyResultProducedV2 integrationEvent)
    {
        return integrationEvent.AggregationLevelCase switch
        {
            EnergyResultProducedV2.AggregationLevelOneofCase.AggregationPerBalanceresponsiblepartyPerGridarea => integrationEvent.AggregationPerBalanceresponsiblepartyPerGridarea.BalanceResponsibleId,
            EnergyResultProducedV2.AggregationLevelOneofCase.AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea => integrationEvent.AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea.BalanceResponsibleId,
            EnergyResultProducedV2.AggregationLevelOneofCase.None => throw new InvalidOperationException("Aggregation level is not specified"),
            _ => null,
        };
    }

    private static string GetGridAreaCodeForAggregationLevel(EnergyResultProducedV2 integrationEvent)
    {
        var gridAreaCode = integrationEvent.AggregationLevelCase switch
        {
            EnergyResultProducedV2.AggregationLevelOneofCase.AggregationPerGridarea => integrationEvent.AggregationPerGridarea.GridAreaCode,
            EnergyResultProducedV2.AggregationLevelOneofCase.AggregationPerBalanceresponsiblepartyPerGridarea => integrationEvent.AggregationPerBalanceresponsiblepartyPerGridarea.GridAreaCode,
            EnergyResultProducedV2.AggregationLevelOneofCase.AggregationPerEnergysupplierPerGridarea => integrationEvent.AggregationPerEnergysupplierPerGridarea.GridAreaCode,
            EnergyResultProducedV2.AggregationLevelOneofCase.AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea => integrationEvent.AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea.GridAreaCode,
            EnergyResultProducedV2.AggregationLevelOneofCase.None => throw new InvalidOperationException(
                "Aggregation level was not specified"),
            _ => throw new InvalidOperationException("Unknown aggregation level"),
        };

        return gridAreaCode;
    }

    private async Task<(string? EnergySupplierNumber, string? BalanceResponsibleNumber, string GridAreaCode, ActorRole ReceiverRole, ActorNumber ReceiverNumber)> GetAggregationLevelDataAsync(EnergyResultProducedV2 integrationEvent, CancellationToken cancellationToken)
    {
        string? energySupplierNumber;
        string? balanceResponsibleNumber;
        string gridAreaCode;
        ActorRole receiverRole;
        ActorNumber receiverNumber;

        switch (integrationEvent.AggregationLevelCase)
        {
            case EnergyResultProducedV2.AggregationLevelOneofCase.AggregationPerGridarea:
                energySupplierNumber = null;
                balanceResponsibleNumber = null;
                gridAreaCode = integrationEvent.AggregationPerGridarea.GridAreaCode;
                receiverRole = ActorRole.MeteredDataResponsible;
                receiverNumber = await GetGridAreaOperatorNumberAsync(gridAreaCode, cancellationToken).ConfigureAwait(false);
                break;
            case EnergyResultProducedV2.AggregationLevelOneofCase.AggregationPerEnergysupplierPerGridarea:
                energySupplierNumber = integrationEvent.AggregationPerEnergysupplierPerGridarea.EnergySupplierId;
                balanceResponsibleNumber = null;
                gridAreaCode = integrationEvent.AggregationPerEnergysupplierPerGridarea.GridAreaCode;
                receiverRole = ActorRole.EnergySupplier;
                receiverNumber = ActorNumber.Create(energySupplierNumber);
                break;
            case EnergyResultProducedV2.AggregationLevelOneofCase.AggregationPerBalanceresponsiblepartyPerGridarea:
                energySupplierNumber = null;
                balanceResponsibleNumber = integrationEvent.AggregationPerBalanceresponsiblepartyPerGridarea.BalanceResponsibleId;
                gridAreaCode = integrationEvent.AggregationPerBalanceresponsiblepartyPerGridarea.GridAreaCode;
                receiverRole = ActorRole.BalanceResponsibleParty;
                receiverNumber = ActorNumber.Create(balanceResponsibleNumber);
                break;
            case EnergyResultProducedV2.AggregationLevelOneofCase.AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea:
                energySupplierNumber = integrationEvent.AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea.EnergySupplierId;
                balanceResponsibleNumber = integrationEvent.AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea.BalanceResponsibleId;
                gridAreaCode = integrationEvent.AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea.GridAreaCode;
                receiverRole = ActorRole.BalanceResponsibleParty;
                receiverNumber = ActorNumber.Create(balanceResponsibleNumber);
                break;
            case EnergyResultProducedV2.AggregationLevelOneofCase.None:
                throw new InvalidOperationException("Aggregation level is not specified");
            default:
                throw new InvalidOperationException("Aggregation level is unknown");
        }

        return (energySupplierNumber, balanceResponsibleNumber, gridAreaCode, receiverRole, receiverNumber);
    }

    private async Task<ActorNumber> GetGridAreaOperatorNumberAsync(
        string gridAreaCode,
        CancellationToken cancellationToken)
    {
        return await _masterDataClient
            .GetGridOwnerForGridAreaCodeAsync(gridAreaCode, cancellationToken)
            .ConfigureAwait(false);
    }
}
