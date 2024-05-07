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

namespace Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;

/// <summary>
/// Represents the total sum of wholesale services for either Energy Supplier or Charge owner.
/// </summary>
public class WholesaleServicesTotalSumMessageDto : WholesaleServicesMessageDto
{
    protected WholesaleServicesTotalSumMessageDto(
        Guid? processId,
        EventId eventId,
        string businessReason,
        ActorNumber receiverNumber,
        ActorRole receiverRole,
        WholesaleServicesSeries series,
        MessageId? relatedToMessageId = null)
        : base(
            receiverNumber,
            processId,
            eventId,
            businessReason,
            receiverRole,
            null,
            series,
            relatedToMessageId)
    {
    }

    public static WholesaleServicesTotalSumMessageDto Create(
        EventId eventId,
        ActorNumber receiverNumber,
        ActorRole receiverRole,
        string businessReason,
        WholesaleServicesTotalSumSeries wholesaleSeries)
    {
        ArgumentNullException.ThrowIfNull(eventId);
        ArgumentNullException.ThrowIfNull(businessReason);

        return new WholesaleServicesTotalSumMessageDto(
            receiverNumber: receiverNumber,
            receiverRole: receiverRole,
            processId: null,
            eventId: eventId,
            businessReason: businessReason,
            series: wholesaleSeries);
    }
}

public record WholesaleServicesTotalSumSeries(
    Guid TransactionId,
    long CalculationVersion,
    string GridAreaCode,
    ActorNumber EnergySupplier,
    Period Period,
    SettlementVersion? SettlementVersion,
    MeasurementUnit PriceMeasureUnit,
    Currency Currency,
    Resolution Resolution,
    decimal Amount) : WholesaleServicesSeries(
    TransactionId: TransactionId,
    CalculationVersion: CalculationVersion,
    GridAreaCode: GridAreaCode,
    ChargeCode: null,
    IsTax: false,
    Points: new[] { new WholesaleServicesPoint(1, null, null, Amount, null), },
    EnergySupplier: EnergySupplier,
    ChargeOwner: null,
    Period: Period,
    SettlementVersion: SettlementVersion,
    QuantityMeasureUnit: null,
    QuantityUnit: null,
    PriceMeasureUnit: PriceMeasureUnit,
    Currency: Currency,
    ChargeType: null,
    MeteringPointType: null,
    SettlementType: null,
    SettlementMethod: null,
    Resolution: Resolution);
