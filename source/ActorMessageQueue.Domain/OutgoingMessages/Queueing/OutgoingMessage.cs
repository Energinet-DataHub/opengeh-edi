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

using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Common.Actors;
using Energinet.DataHub.EDI.Domain;

namespace Energinet.DataHub.EDI.ActorMessageQueue.Domain.OutgoingMessages.Queueing
{
    public class OutgoingMessage
    {
        public OutgoingMessage(DocumentType documentType, ActorNumber receiverId, Guid processId, string businessReason, MarketRole receiverRole, ActorNumber senderId, MarketRole senderRole, string messageRecord)
        {
            DocumentType = documentType;
            ReceiverId = receiverId;
            ProcessId = processId;
            BusinessReason = businessReason;
            ReceiverRole = receiverRole;
            SenderId = senderId;
            SenderRole = senderRole;
            MessageRecord = messageRecord;
            Id = Guid.NewGuid();
        }

        public Guid Id { get; }

        public bool IsPublished { get; private set; }

        public ActorNumber ReceiverId { get; }

        public DocumentType DocumentType { get; }

        public Guid ProcessId { get; }

        public string BusinessReason { get; }

        public MarketRole ReceiverRole { get; }

        public ActorNumber SenderId { get; }

        public MarketRole SenderRole { get; }

        public string MessageRecord { get; }

        public Receiver Receiver => Receiver.Create(ReceiverId, ReceiverRole);

        public BundleId? AssignedBundleId { get; private set; }

        public static OutgoingMessage Create(
            Receiver receiver,
            BusinessReason businessReason,
            DocumentType documentType,
            Guid processId,
            ActorNumber senderId,
            MarketRole senderRole,
            string messageRecord)
        {
            ArgumentNullException.ThrowIfNull(receiver);
            ArgumentNullException.ThrowIfNull(businessReason);

            return new OutgoingMessage(
                documentType,
                receiver.Number,
                processId,
                businessReason.Name,
                receiver.ActorRole,
                senderId,
                senderRole,
                messageRecord);
        }

        public void AssignToBundle(BundleId bundleId)
        {
            AssignedBundleId = bundleId;
        }
    }
}
