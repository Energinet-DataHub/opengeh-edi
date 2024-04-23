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

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;

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
    IReadOnlyCollection<IIncomingMessageSeries> Serie) : IIncomingMessage;

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
    public ActorNumber? GetActorNumberForRole(ActorRole actorRole)
    {
        ArgumentNullException.ThrowIfNull(actorRole);

        // TODO: What are the valid sender roles for RequestWholesaleServicesSeries? Are we missing any below?
        return actorRole.Name switch
        {
            DataHubNames.ActorRole.EnergySupplier => ActorNumber.TryCreate(EnergySupplierId),
            DataHubNames.ActorRole.GridOperator => ActorNumber.TryCreate(ChargeOwner),
            _ => null,
        };
    }
}

public record RequestWholesaleServicesChargeType(string? Id, string? Type);
