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
/// Represents the message for metered data for measurement point known as RSM-012.
/// </summary>
public record MeteredDataForMeasurementPointMessage(
    string MessageId,
    string MessageType,
    string CreatedAt,
    string SenderNumber,
    string ReceiverNumber,
    string SenderRoleCode,
    string BusinessReason,
    string ReceiverRoleCode,
    string? BusinessType,
    IReadOnlyCollection<IIncomingMessageSeries> Series) : IIncomingMessage;

public record MeteredDataForMeasurementPointSeries(
    string TransactionId,
    string? Resolution,
    string StartDateTime,
    string? EndDateTime,
    string? ProductNumber,
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
