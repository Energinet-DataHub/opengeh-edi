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
using Energinet.DataHub.EDI.IncomingMessages.Domain.Abstractions;
using PMTypes = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects;

namespace Energinet.DataHub.EDI.IncomingMessages.Domain;

public record RequestWholesaleServicesMessage(
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
        Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.MessageType.RequestForAggregatedBillingInformation,
    ];

    public IReadOnlyCollection<BusinessReason> AllowedBusinessReasons =>
    [
        Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.BusinessReason.WholesaleFixing,
        Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.BusinessReason.Correction,
    ];

    public IReadOnlyCollection<ActorRole> AllowedSenderRoles => [
        ActorRole.EnergySupplier,
        ActorRole.GridAccessProvider,
        ActorRole.SystemOperator,
    ];
}

public record RequestWholesaleServicesSeries(
    string TransactionId,
    string StartDateTime,
    string? EndDateTime,
    string? GridArea,
    string? EnergySupplierId,
    string? SettlementVersion,
    string? Resolution,
    string? ChargeOwner,
    IReadOnlyCollection<RequestWholesaleServicesChargeType> ChargeTypes) : BaseDelegatedSeries, IIncomingMessageSeries
{
    public ActorNumber? GetActorNumberForRole(ActorRole actorRole, ActorNumber? gridAreaOwner)
    {
        ArgumentNullException.ThrowIfNull(actorRole);

        return PMTypes.ActorRole.FromNameOrDefault(actorRole.Name) switch
        {
            var ar when ar == PMTypes.ActorRole.EnergySupplier => ActorNumber.TryCreate(EnergySupplierId),

            var ar when ar == PMTypes.ActorRole.GridAccessProvider => gridAreaOwner,
            var ar when ar == PMTypes.ActorRole.SystemOperator => ActorNumber.TryCreate(ChargeOwner),
            _ => null,
        };
    }
}

public record RequestWholesaleServicesChargeType(string? Id, string? Type);
