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

using Messaging.Domain.Actors;
using Messaging.Domain.Transactions;

namespace Messaging.Domain.OutgoingMessages
{
    public class OutgoingMessage
    {
        public OutgoingMessage(MessageType messageType, ActorNumber receiverId, TransactionId transactionId, string processType, MarketRole receiverRole, ActorNumber senderId, MarketRole senderRole, string messageRecord)
        {
            MessageType = messageType;
            ReceiverId = receiverId;
            TransactionId = transactionId;
            ProcessType = processType;
            ReceiverRole = receiverRole;
            SenderId = senderId;
            SenderRole = senderRole;
            MessageRecord = messageRecord;
            Id = Guid.NewGuid();
        }

        public Guid Id { get; }

        public bool IsPublished { get; private set; }

        public ActorNumber ReceiverId { get; }

        public MessageType MessageType { get; }

        public TransactionId TransactionId { get; }

        public string ProcessType { get; }

        public MarketRole ReceiverRole { get; }

        public ActorNumber SenderId { get; }

        public MarketRole SenderRole { get; }

        public string MessageRecord { get; }
    }
}
