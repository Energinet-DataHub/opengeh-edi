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

using System.Text.Json;
using Domain.Actors;
using Domain.Documents;
using Domain.OutgoingMessages.Exceptions;
using Domain.Transactions;

namespace Domain.OutgoingMessages.MoveIn.RejectRequestChangeOfSupplier;

public class RejectRequestChangeOfSupplierMessage : OutgoingMessage
{
    private RejectRequestChangeOfSupplierMessage(DocumentType documentType, ActorNumber receiverId, ProcessId processId, string businessReason, MarketRole receiverRole, ActorNumber senderId, MarketRole senderRole, string messageRecord)
        : base(documentType, receiverId, processId, businessReason, receiverRole, senderId, senderRole, messageRecord)
    {
        ArgumentNullException.ThrowIfNull(messageRecord);
        MarketActivityRecord = JsonSerializer.Deserialize<MarketActivityRecord>(messageRecord)!;
    }

    private RejectRequestChangeOfSupplierMessage(ActorNumber receiverId, ProcessId processId, string businessReason, MarketActivityRecord marketActivityRecord)
        : base(DocumentType.RejectRequestChangeOfSupplier, receiverId, processId, businessReason, MarketRole.EnergySupplier, DataHubDetails.IdentificationNumber, MarketRole.MeteringPointAdministrator, JsonSerializer.Serialize(marketActivityRecord))
    {
        MarketActivityRecord = marketActivityRecord;
    }

    public MarketActivityRecord MarketActivityRecord { get; }

    public static RejectRequestChangeOfSupplierMessage Create(
        ProcessId processId,
        ActorProvidedId actorProvidedId,
        BusinessReason businessReason,
        string marketEvaluationPointNumber,
        ActorNumber energySupplierNumber,
        IReadOnlyList<Reason> reasons)
    {
        ArgumentNullException.ThrowIfNull(processId);
        ArgumentNullException.ThrowIfNull(businessReason);
        ArgumentNullException.ThrowIfNull(energySupplierNumber);
        ArgumentNullException.ThrowIfNull(reasons);
        ArgumentNullException.ThrowIfNull(actorProvidedId);

        if (reasons.Count == 0)
        {
            throw new OutgoingMessageException($"Reject message must contain at least one reject reason");
        }

        var marketActivityRecord = new MarketActivityRecord(
            Guid.NewGuid().ToString(),
            actorProvidedId.Id,
            marketEvaluationPointNumber,
            reasons);
        return new RejectRequestChangeOfSupplierMessage(
            energySupplierNumber,
            processId,
            businessReason.Name,
            marketActivityRecord);
    }
}
