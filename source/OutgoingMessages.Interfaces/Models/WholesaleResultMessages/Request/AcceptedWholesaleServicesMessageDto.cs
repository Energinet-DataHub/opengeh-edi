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

namespace Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.WholesaleResultMessages.Request;

public class AcceptedWholesaleServicesMessageDto : OutgoingMessageDto
{
    private AcceptedWholesaleServicesMessageDto(
        ActorNumber receiverNumber,
        Guid processId,
        EventId eventId,
        string businessReason,
        ActorRole receiverRole,
        ActorNumber? chargeOwnerId,
        AcceptedWholesaleServicesSeries series,
        MessageId relatedToMessageId,
        ActorNumber documentReceiverNumber,
        ActorRole documentReceiverRole)
        : base(
            DocumentType.NotifyWholesaleServices,
            receiverNumber,
            processId,
            eventId,
            businessReason,
            receiverRole,
            new ExternalId(Guid.NewGuid()),
            relatedToMessageId)
    {
        ChargeOwnerId = chargeOwnerId;
        DocumentReceiverNumber = documentReceiverNumber;
        DocumentReceiverRole = documentReceiverRole;
        Series = series;
    }

    public AcceptedWholesaleServicesSeries Series { get; }

    public ActorNumber? ChargeOwnerId { get; }

    public ActorNumber DocumentReceiverNumber { get; }

    public ActorRole DocumentReceiverRole { get; }

    public static AcceptedWholesaleServicesMessageDto Create(
        ActorNumber receiverNumber,
        ActorRole receiverRole,
        ActorNumber documentReceiverNumber,
        ActorRole documentReceiverRole,
        ActorNumber? chargeOwnerId,
        Guid processId,
        EventId eventId,
        string businessReason,
        AcceptedWholesaleServicesSeries wholesaleSeries,
        MessageId relatedToMessageId)
    {
        return new AcceptedWholesaleServicesMessageDto(
            receiverNumber: receiverNumber,
            receiverRole: receiverRole,
            documentReceiverNumber: documentReceiverNumber,
            documentReceiverRole: documentReceiverRole,
            processId: processId,
            eventId: eventId,
            businessReason: businessReason,
            series: wholesaleSeries,
            chargeOwnerId: chargeOwnerId,
            relatedToMessageId: relatedToMessageId);
    }
}

public record AcceptedWholesaleServicesSeries(
    TransactionId TransactionId,
    long CalculationVersion,
    string GridAreaCode,
    string? ChargeCode,
    bool IsTax,
    IReadOnlyCollection<WholesaleServicesPoint> Points,
    ActorNumber EnergySupplier,
    ActorNumber? ChargeOwner,
    Period Period,
    SettlementVersion? SettlementVersion,
    MeasurementUnit QuantityMeasureUnit,
    MeasurementUnit PriceMeasureUnit,
    Currency Currency,
    ChargeType? ChargeType,
    Resolution Resolution,
    MeteringPointType? MeteringPointType,
    SettlementMethod? SettlementMethod,
    TransactionId OriginalTransactionIdReference) : WholesaleServicesSeries(
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
    QuantityMeasureUnit,
    null,
    PriceMeasureUnit,
    Currency,
    ChargeType,
    Resolution,
    MeteringPointType,
    null,
    SettlementMethod,
    OriginalTransactionIdReference);
