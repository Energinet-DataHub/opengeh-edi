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

namespace Messaging.Domain.OutgoingMessages.AccountingPointCharacteristics;

public class AccountingPointCharacteristicsMessage : OutgoingMessage
{
    public AccountingPointCharacteristicsMessage(MessageType messageType, ActorNumber receiverId, TransactionId transactionId, string processType, MarketRole receiverRole, ActorNumber senderId, MarketRole senderRole, string messageRecord)
        : base(messageType, receiverId, transactionId, processType, receiverRole, senderId, senderRole, messageRecord)
    {
        ArgumentNullException.ThrowIfNull(messageRecord);
        MarketActivityRecord = JsonSerializer.Deserialize<MarketActivityRecord>(messageRecord)!;
    }

    public AccountingPointCharacteristicsMessage(MessageType messageType, ActorNumber receiverId, TransactionId transactionId, string processType, MarketRole receiverRole, ActorNumber senderId, MarketRole senderRole, MarketActivityRecord marketActivityRecord)
        : base(messageType, receiverId, transactionId, processType, receiverRole, senderId, senderRole, JsonSerializer.Serialize(marketActivityRecord))
    {
        MarketActivityRecord = marketActivityRecord;
    }

    public MarketActivityRecord MarketActivityRecord { get; }
}
