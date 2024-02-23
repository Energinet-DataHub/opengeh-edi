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
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using NodaTime.Serialization.Protobuf;
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
            settlementVersion: aggregatedMeasureDataProcess.SettlementVersion?.Name,
            relatedToMessageId: aggregatedMeasureDataProcess.InitiatedByMessageId,
            originalTransactionIdReference: aggregatedMeasureDataProcess.BusinessTransactionId.Id);
    }

    public async Task<AggregationResultMessage> CreateAsync(
        EnergyResultProducedV2 integrationEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var aggregationData =
            await GetAggregationLevelDataAsync(integrationEvent, cancellationToken).ConfigureAwait(false);

        return AggregationResultMessage.Create(
            receiverNumber: aggregationData.ReceiverNumber,
            receiverRole: aggregationData.ReceiverRole,
            processId: ProcessId.New().Id,
            gridAreaCode: aggregationData.GridAreaCode,
            meteringPointType: MeteringPointTypeMapper.Map(integrationEvent.TimeSeriesType).Name,
            settlementType: SettlementTypeMapper.Map(integrationEvent.TimeSeriesType)?.Name,
            measureUnitType: MeasurementUnitMapper.Map(integrationEvent.QuantityUnit).Name,
            resolution: ResolutionMapper.Map(integrationEvent.Resolution).Name,
            energySupplierNumber: aggregationData.EnergySupplierNumber,
            balanceResponsibleNumber: aggregationData.BalanceResponsibleNumber,
            period: new Period(integrationEvent.PeriodStartUtc.ToInstant(), integrationEvent.PeriodEndUtc.ToInstant()),
            points: TimeSeriesPointsMapper.MapPoints(integrationEvent.TimeSeriesPoints),
            businessReasonName: BusinessReasonMapper.Map(integrationEvent.CalculationType).Name,
            calculationResultVersion: integrationEvent.CalculationResultVersion,
            settlementVersion: SettlementVersionMapper.Map(integrationEvent.CalculationType)?.Name);
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
