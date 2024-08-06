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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.EnergyResultMessages;

/// /// <summary>
/// An outgoing message dto with an energy result for a balance responsible and an energy supplier in a grid area
/// </summary>
public class EnergyResultPerEnergySupplierPerBalanceResponsibleMessageDto
    : OutgoingMessageDto
{
    public EnergyResultPerEnergySupplierPerBalanceResponsibleMessageDto(
        EventId eventId,
        BusinessReason businessReason,
        string gridArea,
        MeteringPointType meteringPointType,
        SettlementMethod? settlementMethod,
        MeasurementUnit measurementUnit,
        Resolution resolution,
        ActorNumber energySupplierNumber,
        ActorNumber balanceResponsibleNumber,
        Period period,
        IReadOnlyCollection<EnergyResultMessagePoint> points,
        long calculationResultVersion,
        SettlementVersion? settlementVersion,
        Guid calculationResultId,
        Guid calculationId)
            : base(
            documentType: DocumentType.NotifyAggregatedMeasureData,
            processId: null,
            eventId: eventId,
            businessReasonName: businessReason.Name,
            receiverNumber: balanceResponsibleNumber,
            receiverRole: ActorRole.BalanceResponsibleParty,
            senderId: DataHubDetails.DataHubActorNumber,
            senderRole: ActorRole.MeteredDataAdministrator,
            externalId: new ExternalId(calculationResultId),
            relatedToMessageId: null)
    {
        CalculationId = calculationId;

        SeriesForBalanceResponsible = new EnergyResultMessageTimeSeries(
            TransactionId.New(),
            gridArea,
            meteringPointType.Name,
            null,
            settlementMethod?.Name,
            measurementUnit.Name,
            resolution.Name,
            energySupplierNumber.Value,
            balanceResponsibleNumber.Value,
            period,
            points,
            calculationResultVersion,
            null,
            settlementVersion?.Name);

        SeriesForEnergySupplier = new EnergyResultMessageTimeSeries(
            TransactionId.New(),
            gridArea,
            meteringPointType.Name,
            null,
            settlementMethod?.Name,
            measurementUnit.Name,
            resolution.Name,
            energySupplierNumber.Value,
            null,
            period,
            points,
            calculationResultVersion,
            null,
            settlementVersion?.Name);

        GridArea = gridArea;
        EnergySupplierNumber = energySupplierNumber;
        BalanceResponsibleNumber = balanceResponsibleNumber;
    }

    public Guid CalculationId { get; }

    public EnergyResultMessageTimeSeries SeriesForBalanceResponsible { get; }

    public EnergyResultMessageTimeSeries SeriesForEnergySupplier { get; }

    public string GridArea { get; }

    public ActorNumber EnergySupplierNumber { get; }

    public ActorNumber BalanceResponsibleNumber { get; }
}
