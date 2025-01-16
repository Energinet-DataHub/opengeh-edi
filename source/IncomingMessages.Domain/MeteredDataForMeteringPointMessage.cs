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
using Energinet.DataHub.EDI.IncomingMessages.Domain.Abstractions;

namespace Energinet.DataHub.EDI.IncomingMessages.Domain;

/// <summary>
/// Represents the message for metered data for metering point known as RSM-012.
/// </summary>
public class MeteredDataForMeteringPointMessageBase(
    string messageId,
    string messageType,
    string createdAt,
    string senderNumber,
    string receiverNumber,
    string senderRoleCode,
    string businessReason,
    string receiverRoleCode,
    string? businessType,
    IReadOnlyCollection<IIncomingMessageSeries> series) : IIncomingMessage
{
    public string MessageId { get; } = messageId;

    public string ReceiverNumber { get; } = receiverNumber;

    public string ReceiverRoleCode { get; } = receiverRoleCode;

    public string SenderNumber { get; } = senderNumber;

    public string SenderRoleCode { get; } = senderRoleCode;

    public string BusinessReason { get; } = businessReason;

    public string MessageType { get; } = messageType;

    public string CreatedAt { get; } = createdAt;

    public string? BusinessType { get; } = businessType;

    public IReadOnlyCollection<IIncomingMessageSeries> Series { get; } = series;

    public IReadOnlyCollection<MessageType> AllowedMessageTypes => [
        Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.MessageType.ValidatedMeteredData,
    ];

    public IReadOnlyCollection<BusinessReason> AllowedBusinessReasons => [
        Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.BusinessReason.PeriodicMetering,
    ];

    public IReadOnlyCollection<ActorRole> AllowedSenderRoles => [
        ActorRole.MeteredDataResponsible,
    ];
}

public record MeteredDataForMeteringPointSeries(
    string TransactionId,
    string? Resolution,
    string StartDateTime,
    string? EndDateTime,
    string? ProductNumber,
    // This field is not used in Ebix
    string? RegisteredAt,
    string? ProductUnitType,
    string? MeteringPointType,
    string? MeteringPointLocationId,
    string SenderNumber,
    IReadOnlyCollection<EnergyObservation> EnergyObservations) : BaseDelegatedSeries, IIncomingMessageSeries
{
    // TODO: when refactor incoming message module not all incomingMessageSeries has a gridArea.
    public string? GridArea => null;

    public ActorNumber? GetActorNumberForRole(ActorRole actorRole, ActorNumber? gridAreaOwner)
    {
        return actorRole.Name switch
        {
            DataHubNames.ActorRole.GridAccessProvider => ActorNumber.TryCreate(SenderNumber),
            DataHubNames.ActorRole.Delegated => ActorNumber.TryCreate(SenderNumber),
            _ => null,
        };
    }
}

public record EnergyObservation(
    string? Position,
    string? EnergyQuantity,
    string? QuantityQuality);
