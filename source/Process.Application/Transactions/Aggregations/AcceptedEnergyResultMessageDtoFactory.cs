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

using System;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.Process.Application.Transactions.Mappers;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace Energinet.DataHub.EDI.Process.Application.Transactions.Aggregations;

public static class AcceptedEnergyResultMessageDtoFactory
{
    public static AcceptedEnergyResultMessageDto Create(
        AggregatedMeasureDataProcess aggregatedMeasureDataProcess,
        AcceptedEnergyResultTimeSerie acceptedEnergyResultTimeSerie)
    {
        ArgumentNullException.ThrowIfNull(aggregatedMeasureDataProcess);
        ArgumentNullException.ThrowIfNull(acceptedEnergyResultTimeSerie);

        return AcceptedEnergyResultMessageDto.Create(
            receiverNumber: aggregatedMeasureDataProcess.RequestedByActorId,
            receiverRole: ActorRole.FromCode(aggregatedMeasureDataProcess.RequestedByActorRoleCode),
            processId: aggregatedMeasureDataProcess.ProcessId.Id,
            gridAreaCode: acceptedEnergyResultTimeSerie.GridAreaDetails.GridAreaCode,
            acceptedEnergyResultTimeSerie.MeteringPointType,
            aggregatedMeasureDataProcess.SettlementMethod,
            acceptedEnergyResultTimeSerie.UnitType,
            acceptedEnergyResultTimeSerie.Resolution,
            energySupplierNumber: aggregatedMeasureDataProcess.EnergySupplierId,
            balanceResponsibleNumber: aggregatedMeasureDataProcess.BalanceResponsibleId,
            period: new Period(acceptedEnergyResultTimeSerie.StartOfPeriod, acceptedEnergyResultTimeSerie.EndOfPeriod),
            points: AcceptedEnergyResultMessageMapper.MapPoints(acceptedEnergyResultTimeSerie.Points),
            businessReasonName: aggregatedMeasureDataProcess.BusinessReason.Name,
            calculationResultVersion: acceptedEnergyResultTimeSerie.CalculationResultVersion,
            settlementVersion: aggregatedMeasureDataProcess.SettlementVersion?.Name,
            relatedToMessageId: aggregatedMeasureDataProcess.InitiatedByMessageId,
            originalTransactionIdReference: aggregatedMeasureDataProcess.BusinessTransactionId.Id);
    }

    public static AcceptedEnergyResultMessageDto Create(
        AggregatedMeasureDataProcess aggregatedMeasureDataProcess,
        AcceptedEnergyResultTimeSeries acceptedEnergyResultTimeSerie)
    {
        ArgumentNullException.ThrowIfNull(aggregatedMeasureDataProcess);
        ArgumentNullException.ThrowIfNull(acceptedEnergyResultTimeSerie);

        return AcceptedEnergyResultMessageDto.Create(
            aggregatedMeasureDataProcess.RequestedByActorId,
            ActorRole.FromCode(aggregatedMeasureDataProcess.RequestedByActorRoleCode),
            aggregatedMeasureDataProcess.ProcessId.Id,
            acceptedEnergyResultTimeSerie.GridAreaDetails.GridAreaCode,
            acceptedEnergyResultTimeSerie.MeteringPointType.Name,
            acceptedEnergyResultTimeSerie.SettlementType?.Name,
            acceptedEnergyResultTimeSerie.UnitType.Name,
            acceptedEnergyResultTimeSerie.Resolution.Name,
            aggregatedMeasureDataProcess.EnergySupplierId,
            aggregatedMeasureDataProcess.BalanceResponsibleId,
            new Period(acceptedEnergyResultTimeSerie.StartOfPeriod, acceptedEnergyResultTimeSerie.EndOfPeriod),
            AcceptedEnergyResultMessageMapper.MapPoints(acceptedEnergyResultTimeSerie.Points),
            aggregatedMeasureDataProcess.BusinessReason.Name,
            acceptedEnergyResultTimeSerie.CalculationResultVersion,
            settlementVersion: aggregatedMeasureDataProcess.SettlementVersion?.Name,
            relatedToMessageId: aggregatedMeasureDataProcess.InitiatedByMessageId,
            originalTransactionIdReference: aggregatedMeasureDataProcess.BusinessTransactionId.Id);
    }
}
