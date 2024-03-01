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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Common.Serialization;

namespace Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;

public class WholesaleServicesMessageDto : OutgoingMessageDto
{
    private WholesaleServicesMessageDto(
        ActorNumber receiverId,
        Guid processId,
        BusinessReason businessReason,
        ActorRole receiverRole,
        ActorNumber chargeOwnerId,
        WholesaleServicesSeries series)
        : base(
            DocumentType.NotifyWholesaleServices,
            receiverId,
            processId,
            businessReason.Name,
            receiverRole,
            DataHubDetails.DataHubActorNumber,
            ActorRole.MeteredDataAdministrator,
            new Serializer().Serialize(series))
    {
        ChargeOwnerId = chargeOwnerId;
    }

    public ActorNumber ChargeOwnerId { get; }

    public static WholesaleServicesMessageDto Create(
        ActorNumber receiverNumber,
        ActorRole receiverRole,
        ActorNumber chargeOwnerId,
        Guid processId,
        BusinessReason businessReason,
        WholesaleServicesSeries wholesaleSeries)
    {
        ArgumentNullException.ThrowIfNull(processId);
        ArgumentNullException.ThrowIfNull(businessReason);

        return new WholesaleServicesMessageDto(
            receiverId: receiverNumber,
            receiverRole: receiverRole,
            processId: processId,
            businessReason: businessReason,
            series: wholesaleSeries,
            chargeOwnerId: chargeOwnerId);
    }
}

public record WholesaleServicesSeries(
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
    SettlementType? SettlementType);

public record WholesaleServicesPoint(int Position, decimal? Quantity, decimal? Price, decimal? Amount, CalculatedQuantityQuality? QuantityQuality);
