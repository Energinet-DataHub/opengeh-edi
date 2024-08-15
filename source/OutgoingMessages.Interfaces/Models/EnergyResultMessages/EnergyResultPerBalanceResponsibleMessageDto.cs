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

/// <summary>
/// An outgoing message dto with an energy result for a balance responsible in a grid area
/// </summary>
public class EnergyResultPerBalanceResponsibleMessageDto
    : OutgoingMessageDto
{
    public EnergyResultPerBalanceResponsibleMessageDto(
        EventId eventId,
        BusinessReason businessReason,
        string gridArea,
        MeteringPointType meteringPointType,
        SettlementMethod? settlementMethod,
        MeasurementUnit measurementUnit,
        Resolution resolution,
        ActorNumber balanceResponsibleNumber,
        Period period,
        IReadOnlyCollection<EnergyResultMessagePoint> points,
        long calculationResultVersion,
        SettlementVersion? settlementVersion,
        Guid calculationResultId,
        Guid calculationId)
            : base(
            documentType: DocumentType.NotifyAggregatedMeasureData,
            receiverNumber: balanceResponsibleNumber,
            processId: null,
            eventId: eventId,
            businessReasonName: businessReason.Name,
            receiverRole: ActorRole.BalanceResponsibleParty,
            externalId: new ExternalId(calculationResultId),
            relatedToMessageId: null)
    {
        CalculationId = calculationId;

        Series = new EnergyResultMessageTimeSeries(
            TransactionId.New(),
            gridArea,
            meteringPointType.Name,
            settlementMethod?.Name,
            measurementUnit.Name,
            resolution.Name,
            null,
            balanceResponsibleNumber.Value,
            period,
            points,
            calculationResultVersion,
            null,
            settlementVersion?.Name);
    }

    public Guid CalculationId { get; }

    public EnergyResultMessageTimeSeries Series { get; }
}
