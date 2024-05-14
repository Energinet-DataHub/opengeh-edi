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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.Process.Application.Transactions.Mappers;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace Energinet.DataHub.EDI.Process.Application.Transactions.Aggregations;

public static class AcceptedEnergyResultMessageDtoFactory
{
    public static AcceptedEnergyResultMessageDto Create(
        EventId eventId,
        AggregatedMeasureDataProcess aggregatedMeasureDataProcess,
        AcceptedEnergyResultTimeSeries acceptedEnergyResultTimeSeries)
    {
        ArgumentNullException.ThrowIfNull(aggregatedMeasureDataProcess);
        ArgumentNullException.ThrowIfNull(acceptedEnergyResultTimeSeries);

        return AcceptedEnergyResultMessageDto.Create(
            receiverNumber: aggregatedMeasureDataProcess.RequestedByActor.ActorNumber,
            receiverRole: aggregatedMeasureDataProcess.RequestedByActor.ActorRole,
            processId: aggregatedMeasureDataProcess.ProcessId.Id,
            eventId: eventId,
            gridAreaCode: acceptedEnergyResultTimeSeries.GridAreaCode,
            meteringPointType: acceptedEnergyResultTimeSeries.MeteringPointType.Name,
            settlementMethod: acceptedEnergyResultTimeSeries.SettlementMethod?.Name,
            measureUnitType: acceptedEnergyResultTimeSeries.UnitType.Name,
            resolution: acceptedEnergyResultTimeSeries.Resolution.Name,
            energySupplierNumber: aggregatedMeasureDataProcess.EnergySupplierId,
            balanceResponsibleNumber: aggregatedMeasureDataProcess.BalanceResponsibleId,
            period: new Period(acceptedEnergyResultTimeSeries.StartOfPeriod, acceptedEnergyResultTimeSeries.EndOfPeriod),
            points: AcceptedEnergyResultMessageMapper.MapPoints(acceptedEnergyResultTimeSeries.Points),
            businessReasonName: aggregatedMeasureDataProcess.BusinessReason.Name,
            calculationResultVersion: acceptedEnergyResultTimeSeries.CalculationResultVersion,
            settlementVersion: aggregatedMeasureDataProcess.SettlementVersion?.Name,
            relatedToMessageId: aggregatedMeasureDataProcess.InitiatedByMessageId,
            originalTransactionIdReference: aggregatedMeasureDataProcess.BusinessTransactionId,
            documentReceiverNumber: aggregatedMeasureDataProcess.OriginalActor.ActorNumber,
            documentReceiverRole: aggregatedMeasureDataProcess.OriginalActor.ActorRole);
    }
}
