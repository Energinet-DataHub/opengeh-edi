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
using Messaging.Domain.Actors;
using Messaging.Domain.Transactions;

namespace Messaging.Domain.OutgoingMessages.ConfirmRequestChangeOfSupplier;

public class ConfirmRequestChangeOfSupplierMessage : OutgoingMessage
{
    private ConfirmRequestChangeOfSupplierMessage(MessageType messageType, ActorNumber receiverId, TransactionId transactionId, string processType, MarketRole receiverRole, ActorNumber senderId, MarketRole senderRole, string messageRecord)
        : base(messageType, receiverId, transactionId, processType, receiverRole, senderId, senderRole, messageRecord)
    {
        ArgumentNullException.ThrowIfNull(messageRecord);
        MarketActivityRecord = JsonSerializer.Deserialize<MarketActivityRecord>(messageRecord)!;
    }

    private ConfirmRequestChangeOfSupplierMessage(ActorNumber receiverId, TransactionId transactionId, string processType, MarketActivityRecord marketActivityRecord)
        : base(MessageType.ConfirmRequestChangeOfSupplier, receiverId, transactionId, processType, MarketRole.EnergySupplier, DataHubDetails.IdentificationNumber, MarketRole.MeteringPointAdministrator, JsonSerializer.Serialize(marketActivityRecord))
    {
        MarketActivityRecord = marketActivityRecord;
    }

    public MarketActivityRecord MarketActivityRecord { get; }

    public static OutgoingMessage Create(
        TransactionId transactionId,
        ActorProvidedId actorProvidedId,
        ProcessType processType,
        string marketEvaluationPointNumber,
        ActorNumber energySupplierNumber)
    {
        ArgumentNullException.ThrowIfNull(processType);
        ArgumentNullException.ThrowIfNull(actorProvidedId);

        var marketActivityRecord = new MarketActivityRecord(
            Guid.NewGuid().ToString(),
            actorProvidedId.Id,
            marketEvaluationPointNumber);

        return new ConfirmRequestChangeOfSupplierMessage(
            energySupplierNumber,
            transactionId,
            processType.Code,
            marketActivityRecord);
    }
}
