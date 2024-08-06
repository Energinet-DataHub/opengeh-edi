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

namespace Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.EnergyResultMessages.Request;

public class AcceptedEnergyResultMessageDto : OutgoingMessageDto
{
    private AcceptedEnergyResultMessageDto(
        ActorNumber receiverNumber,
        Guid processId,
        EventId eventId,
        string businessReason,
        ActorRole receiverRole,
        AcceptedEnergyResultMessageTimeSeries series,
        MessageId? relatedToMessageId,
        ActorNumber documentReceiverNumber,
        ActorRole documentReceiverRole)
        : base(
            DocumentType.NotifyAggregatedMeasureData,
            receiverNumber,
            processId,
            eventId,
            businessReason,
            receiverRole,
            DataHubDetails.DataHubActorNumber,
            ActorRole.MeteredDataAdministrator,
            new ExternalId(Guid.NewGuid()),
            relatedToMessageId)
    {
        Series = series;
        DocumentReceiverNumber = documentReceiverNumber;
        DocumentReceiverRole = documentReceiverRole;
    }

    public ActorNumber DocumentReceiverNumber { get; }

    public ActorRole DocumentReceiverRole { get; }

    public AcceptedEnergyResultMessageTimeSeries Series { get; }

    public static AcceptedEnergyResultMessageDto Create(
        ActorNumber receiverNumber,
        ActorRole receiverRole,
        ActorNumber documentReceiverNumber,
        ActorRole documentReceiverRole,
        Guid processId,
        EventId eventId,
        string gridAreaCode,
        string meteringPointType,
        string? settlementMethod,
        string measureUnitType,
        string resolution,
        string? energySupplierNumber,
        string? balanceResponsibleNumber,
        Period period,
        IReadOnlyCollection<AcceptedEnergyResultMessagePoint> points,
        string businessReasonName,
        long calculationResultVersion,
        TransactionId? originalTransactionIdReference,
        string? settlementVersion,
        MessageId? relatedToMessageId)
    {
        var series = new AcceptedEnergyResultMessageTimeSeries(
            TransactionId: TransactionId.New(),
            gridAreaCode,
            meteringPointType,
            null,
            settlementMethod,
            measureUnitType,
            resolution,
            energySupplierNumber,
            balanceResponsibleNumber,
            period,
            points.Select(p => new AcceptedEnergyResultMessagePoint(p.Position, p.Quantity, p.QuantityQuality, p.SampleTime)).ToList(),
            calculationResultVersion,
            originalTransactionIdReference,
            settlementVersion);
        return new AcceptedEnergyResultMessageDto(
            receiverNumber,
            processId,
            eventId,
            businessReasonName,
            receiverRole,
            series,
            relatedToMessageId,
            documentReceiverNumber,
            documentReceiverRole);
    }
}

public record AcceptedEnergyResultMessageTimeSeries(
    TransactionId TransactionId,
    string GridAreaCode,
    string MeteringPointType,
    string? SettlementType, // TODO: To ensure backwards compatibility, will be remove in another PR.
    string? SettlementMethod,
    string MeasureUnitType,
    string Resolution,
    string? EnergySupplierNumber,
    string? BalanceResponsibleNumber,
    Period Period,
    IReadOnlyCollection<AcceptedEnergyResultMessagePoint> Point,
    long CalculationResultVersion,
    TransactionId? OriginalTransactionIdReference = null,
    string? SettlementVersion = null);

public record AcceptedEnergyResultMessagePoint(int Position, decimal? Quantity, CalculatedQuantityQuality QuantityQuality, string SampleTime);
