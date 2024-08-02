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

namespace Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.WholesaleResultMessages;

/// <summary>
/// Represents the total sum of wholesale services for either Energy Supplier or Charge owner.
/// </summary>
public class WholesaleServicesTotalSumMessageDto : OutgoingMessageDto
{
    private WholesaleServicesTotalSumMessageDto(
        Guid? processId,
        EventId eventId,
        Guid calculationId,
        BusinessReason businessReason,
        ActorNumber receiverNumber,
        ActorRole receiverRole,
        WholesaleServicesTotalSumSeries series,
        MessageId? relatedToMessageId = null)
        : base(
            DocumentType.NotifyWholesaleServices,
            receiverNumber,
            processId,
            eventId,
#pragma warning disable CA1062
            businessReason.Name,
#pragma warning restore CA1062
            receiverRole,
            DataHubDetails.DataHubActorNumber,
            ActorRole.MeteredDataAdministrator,
            new ExternalId(Guid.NewGuid()),
            relatedToMessageId)
    {
        CalculationId = calculationId;
        Series = series;
    }

    public Guid CalculationId { get; }

    public WholesaleServicesTotalSumSeries Series { get; }

    public static WholesaleServicesTotalSumMessageDto Create(
        EventId eventId,
        Guid calculationId,
        ActorNumber receiverNumber,
        ActorRole receiverRole,
        BusinessReason businessReason,
        WholesaleServicesTotalSumSeries wholesaleSeries)
    {
        ArgumentNullException.ThrowIfNull(eventId);
        ArgumentNullException.ThrowIfNull(businessReason);

        return new WholesaleServicesTotalSumMessageDto(
            receiverNumber: receiverNumber,
            receiverRole: receiverRole,
            processId: null,
            eventId: eventId,
            calculationId: calculationId,
            businessReason: businessReason,
            series: wholesaleSeries);
    }
}

public record WholesaleServicesTotalSumSeries(
    TransactionId TransactionId,
    long CalculationVersion,
    string GridAreaCode,
    ActorNumber EnergySupplier,
    Period Period,
    SettlementVersion? SettlementVersion,
    MeasurementUnit QuantityMeasureUnit,
    Currency Currency,
    Resolution Resolution,
    decimal? Amount) : WholesaleServicesSeries(
    TransactionId: TransactionId,
    CalculationVersion: CalculationVersion,
    GridAreaCode: GridAreaCode,
    ChargeCode: null,
    IsTax: false,
    Points: new[] { new WholesaleServicesPoint(1, null, null, Amount, Amount != null ? CalculatedQuantityQuality.Calculated : CalculatedQuantityQuality.Missing), },
    EnergySupplier: EnergySupplier,
    ChargeOwner: null,
    Period: Period,
    SettlementVersion: SettlementVersion,
    QuantityMeasureUnit: QuantityMeasureUnit,
    QuantityUnit: null,
    PriceMeasureUnit: null,
    Currency: Currency,
    ChargeType: null,
    MeteringPointType: null,
    SettlementType: null,
    SettlementMethod: null,
    Resolution: Resolution);
