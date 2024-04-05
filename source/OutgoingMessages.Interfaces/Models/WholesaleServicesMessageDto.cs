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

namespace Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;

public class WholesaleServicesMessageDto : OutgoingMessageDto
{
    protected WholesaleServicesMessageDto(
        ActorNumber receiverNumber,
        Guid processId,
        string businessReason,
        ActorRole receiverRole,
        ActorNumber chargeOwnerId,
        WholesaleServicesSeries series,
        MessageId? relatedToMessageId = null)
        : base(
            DocumentType.NotifyWholesaleServices,
            receiverNumber,
            processId,
            businessReason,
            receiverRole,
            DataHubDetails.DataHubActorNumber,
            ActorRole.MeteredDataAdministrator,
            relatedToMessageId)
    {
        ChargeOwnerId = chargeOwnerId;
        Series = series;
    }

    public ActorNumber ChargeOwnerId { get; }

    public WholesaleServicesSeries Series { get; }

    public static WholesaleServicesMessageDto Create(
        ActorNumber receiverNumber,
        ActorRole receiverRole,
        ActorNumber chargeOwnerId,
        Guid processId,
        string businessReason,
        WholesaleServicesSeries wholesaleSeries)
    {
        ArgumentNullException.ThrowIfNull(processId);
        ArgumentNullException.ThrowIfNull(businessReason);

        return new WholesaleServicesMessageDto(
            receiverNumber: receiverNumber,
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
    MeasurementUnit QuantityMeasureUnit,
    MeasurementUnit? QuantityUnit, // To ensure backwards compatibility, will be remove in another PR.
    MeasurementUnit PriceMeasureUnit,
    Currency Currency,
    ChargeType ChargeType,
    Resolution Resolution,
    MeteringPointType? MeteringPointType,
    SettlementMethod? SettlementType, // TODO: To ensure backwards compatibility, will be removed in another PR.
    SettlementMethod? SettlementMethod,
    string? OriginalTransactionIdReference = null);

public record WholesaleServicesPoint(int Position, decimal? Quantity, decimal? Price, decimal? Amount, CalculatedQuantityQuality? QuantityQuality);
