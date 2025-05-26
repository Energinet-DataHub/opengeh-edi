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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using PMCoreTypes = Energinet.DataHub.ProcessManager.Abstractions.Core.ValueObjects;

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;

/// <summary>
/// Represents the message for requesting measurements for metering points known as RSM-015.
/// </summary>
public class RequestMeasurementsMessageBase(
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
        BuildingBlocks.Domain.Models.MessageType.RequestValidatedMeasurements,
    ];

    public IReadOnlyCollection<BusinessReason> AllowedBusinessReasons => [
        BuildingBlocks.Domain.Models.BusinessReason.PeriodicMetering,
    ];

    public IReadOnlyCollection<ActorRole> AllowedSenderRoles => [
        ActorRole.EnergySupplier,
        ActorRole.Delegated,
        ActorRole.GridAccessProvider,
        ActorRole.MeteredDataResponsible,
        ActorRole.SystemOperator,
        ActorRole.DanishEnergyAgency,
    ];

    public IReadOnlyList<MeteringPointId> MeteringPointIds => Series
        .Cast<RequestValidatedMeasurementsSeries>()
        .Select(x => x.MeteringPointId)
        .ToList();
}

public record RequestValidatedMeasurementsSeries(
    string TransactionId,
    string StartDateTime,
    string? EndDateTime,
    MeteringPointId MeteringPointId,
    string SenderNumber) : IIncomingMessageSeries
{
    public string? GridArea => null;

    public bool IsDelegated => false;

    public ActorNumber? OriginalActorNumber => null;

    public ActorRole? RequestedByActorRole => null;

    public IReadOnlyCollection<string> DelegatedGridAreas { get; } = Array.Empty<string>();

    public void DelegateSeries(
        ActorNumber? originalActorNumber,
        ActorRole requestedByActorRole,
        IReadOnlyCollection<string> delegatedGridAreas)
    {
        throw new NotImplementedException($"Delegation for {typeof(RequestValidatedMeasurementsSeries)} is not handled in EDI.");
    }

    public ActorNumber? GetActorNumberForRole(ActorRole actorRole, ActorNumber? gridAreaOwner)
    {
        throw new NotImplementedException($"Delegation for {typeof(RequestValidatedMeasurementsSeries)} is not handled in EDI.");
    }
}
