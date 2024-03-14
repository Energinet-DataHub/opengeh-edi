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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;

public class AcceptedWholesaleServicesMessageDto : WholesaleServicesMessageDto
{
    protected AcceptedWholesaleServicesMessageDto(
        ActorNumber receiverId,
        Guid processId,
        string businessReason,
        ActorRole receiverRole,
        ActorNumber chargeOwnerId,
        AcceptedWholesaleServicesSeries series,
        MessageId relatedToMessageId)
        : base(
        receiverId,
        processId,
        businessReason,
        receiverRole,
        chargeOwnerId,
        series,
        relatedToMessageId)
    {
    }

    public static AcceptedWholesaleServicesMessageDto Create(
        ActorNumber receiverNumber,
        ActorRole receiverRole,
        ActorNumber chargeOwnerId,
        Guid processId,
        string businessReason,
        AcceptedWholesaleServicesSeries wholesaleSeries,
        MessageId relatedToMessageId)
    {
        ArgumentNullException.ThrowIfNull(processId);
        ArgumentNullException.ThrowIfNull(businessReason);

        return new AcceptedWholesaleServicesMessageDto(
            receiverId: receiverNumber,
            receiverRole: receiverRole,
            processId: processId,
            businessReason: businessReason,
            series: wholesaleSeries,
            chargeOwnerId: chargeOwnerId,
            relatedToMessageId: relatedToMessageId);
    }
}

public record AcceptedWholesaleServicesSeries(
    Guid TransactionId,
    long CalculationVersion,
    string GridAreaCode,
    string ChargeCode,
    bool IsTax,
    IReadOnlyCollection<WholesaleServicesPoint> Points,
    ActorNumber EnergySupplier,
    ActorNumber ChargeOwner,
    Period Period,
    SettlementVersion? SettlementVersion,
    MeasurementUnit QuantityUnit,
    MeasurementUnit PriceMeasureUnit,
    Currency Currency,
    ChargeType ChargeType,
    Resolution Resolution,
    MeteringPointType? MeteringPointType,
    SettlementType? SettlementType,
    string OriginalTransactionIdReference) : WholesaleServicesSeries(
    TransactionId,
    CalculationVersion,
    GridAreaCode,
    ChargeCode,
    IsTax,
    Points,
    EnergySupplier,
    ChargeOwner,
    Period,
    SettlementVersion,
    QuantityUnit,
    PriceMeasureUnit,
    Currency,
    ChargeType,
    Resolution,
    MeteringPointType,
    SettlementType);
