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

namespace Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;

public class WholesaleTotalAmountMessageDto : OutgoingMessageDto
{
    public WholesaleTotalAmountMessageDto(
        EventId eventId,
        Guid calculationResultId,
        long calculationResultVersion,
        ActorNumber receiverNumber,
        ActorRole receiverRole,
        ActorNumber energySupplierId,
        string businessReason,
        string gridAreaCode,
        Period period,
        Currency currency,
        SettlementVersion? settlementVersion,
        IReadOnlyCollection<WholesaleServicesPoint> points)
        : base(
            documentType: DocumentType.NotifyWholesaleServices,
            receiverNumber: receiverNumber,
            null,
            eventId,
            businessReason,
            receiverRole: receiverRole,
            senderId: DataHubDetails.DataHubActorNumber,
            senderRole: ActorRole.MeteredDataAdministrator,
            new ExternalId(calculationResultId))
    {
        CalculationResultId = calculationResultId;

        Series = new WholesaleServicesSeries(
            TransactionId: TransactionId.New(),
            CalculationVersion: calculationResultVersion,
            GridAreaCode: gridAreaCode,
            ChargeCode: null,
            IsTax: false,
            Points: points,
            EnergySupplier: energySupplierId,
            ChargeOwner: null,
            Period: period,
            SettlementVersion: settlementVersion,
            QuantityMeasureUnit: MeasurementUnit.Kwh,
            null,
            PriceMeasureUnit: null,
            Currency: currency,
            ChargeType: null,
            Resolution: Resolution.Monthly,
            MeteringPointType: null,
            SettlementType: null,
            SettlementMethod: null);
    }

    public Guid CalculationResultId { get; }

    public WholesaleServicesSeries Series { get; init; }
}
