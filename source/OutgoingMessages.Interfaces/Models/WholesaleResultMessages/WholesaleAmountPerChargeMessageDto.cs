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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.WholesaleResultMessages;

/// <summary>
/// An outgoing message dto with a wholesale result with amount per charge
/// for an energy supplier and grid owner in a grid area
/// </summary>
public class WholesaleAmountPerChargeMessageDto : OutgoingMessageDto
{
    public WholesaleAmountPerChargeMessageDto(
        EventId eventId,
        Guid calculationId,
        Guid calculationResultId,
        long calculationResultVersion,
        ActorNumber energySupplierReceiverId,
        ActorNumber chargeOwnerReceiverId,
        ActorNumber chargeOwnerId,
        string businessReason,
        string gridAreaCode,
        bool isTax,
        Period period,
        MeasurementUnit quantityUnit,
        Currency currency,
        ChargeType chargeType,
        Resolution resolution,
        SettlementVersion? settlementVersion,
        MeteringPointType? meteringPointType,
        SettlementMethod? settlementMethod,
        string? chargeCode,
        IReadOnlyCollection<WholesaleServicesPoint> points)
        : base(
            documentType: DocumentType.NotifyWholesaleServices,
            receiverNumber: null!,
            processId: null,
            eventId: eventId,
            businessReasonName: businessReason,
            receiverRole: null!,
            externalId: new ExternalId(calculationResultId))
    {
        CalculationId = calculationId;
        EnergySupplierReceiverId = energySupplierReceiverId;
        ChargeOwnerReceiverId = chargeOwnerReceiverId;

        Series = new WholesaleServicesSeries(
            TransactionId: TransactionId.New(),
            CalculationVersion: calculationResultVersion,
            GridAreaCode: gridAreaCode,
            ChargeCode: chargeCode,
            IsTax: isTax,
            Points: points,
            EnergySupplier: energySupplierReceiverId,
            chargeOwnerId,
            Period: period,
            SettlementVersion: settlementVersion,
            quantityUnit,
            PriceMeasureUnit: MeasurementUnit.TryFromChargeType(chargeType),
            Currency: currency,
            ChargeType: chargeType,
            Resolution: resolution,
            MeteringPointType: meteringPointType,
            SettlementMethod: settlementMethod);
    }

    public Guid CalculationId { get; }

    public ActorNumber EnergySupplierReceiverId { get; }

    public ActorNumber ChargeOwnerReceiverId { get; }

    public WholesaleServicesSeries Series { get; init; }
}
