﻿// Copyright 2020 Energinet DataHub A/S
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

public record RequestAggregatedMeasureDataMessage(
    string SenderNumber,
    string SenderRoleCode,
    string ReceiverNumber,
    string ReceiverRoleCode,
    string BusinessReason,
    string MessageType,
    string MessageId,
    string CreatedAt,
    string? BusinessType,
    IReadOnlyCollection<IIncomingMessageSeries> Series) : IIncomingMessage
{
    public IReadOnlyCollection<MessageType> AllowedMessageTypes => [
        BuildingBlocks.Domain.Models.MessageType.RequestAggregatedMeteredData,
    ];

    public IReadOnlyCollection<BusinessReason> AllowedBusinessReasons =>
    [
        BuildingBlocks.Domain.Models.BusinessReason.PreliminaryAggregation,
        BuildingBlocks.Domain.Models.BusinessReason.BalanceFixing,
        BuildingBlocks.Domain.Models.BusinessReason.WholesaleFixing,
        BuildingBlocks.Domain.Models.BusinessReason.Correction,
    ];

    public IReadOnlyCollection<ActorRole> AllowedSenderRoles => [
        ActorRole.EnergySupplier,
        ActorRole.MeteredDataResponsible,
        ActorRole.BalanceResponsibleParty,
    ];

    public IReadOnlyList<MeteringPointId> MeteringPointIds => Array.Empty<MeteringPointId>();
}

public record RequestAggregatedMeasureDataMessageSeries(
    string TransactionId,
    string? MeteringPointType,
    string? SettlementMethod,
    string StartDateTime,
    string? EndDateTime,
    string? GridArea,
    string? EnergySupplierId,
    string? BalanceResponsiblePartyId,
    string? SettlementVersion) : BaseDelegatedSeries, IIncomingMessageSeries
{
    public ActorNumber? GetActorNumberForRole(ActorRole actorRole, ActorNumber? gridAreaOwner)
    {
        ArgumentNullException.ThrowIfNull(actorRole);

        // Roles who can make a request for aggregated measure data:
        // ActorRole.EnergySupplier,
        // ActorRole.MeteredDataResponsible,
        // ActorRole.BalanceResponsibleParty,
        // ActorRole.GridOperator, // Grid Operator can make requests because of DDM -> MDR hack
        return actorRole.Name switch
        {
            var name when name == PMCoreTypes.ActorRole.EnergySupplier.Name => ActorNumber.TryCreate(EnergySupplierId),
            var name when name == PMCoreTypes.ActorRole.BalanceResponsibleParty.Name => ActorNumber.TryCreate(BalanceResponsiblePartyId),
            var name when name == PMCoreTypes.ActorRole.MeteredDataResponsible.Name => gridAreaOwner,
            var name when name == PMCoreTypes.ActorRole.GridAccessProvider.Name => gridAreaOwner,
            _ => null,
        };
    }
}
