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

using System.Collections.Generic;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundels;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages
{
    public class OutgoingMessageBundle
    {
        public OutgoingMessageBundle(
            DocumentType documentType,
            Receiver receiver,
            Receiver documentReceiver,
            string businessReason,
            ActorNumber senderId,
            ActorRole senderRole,
            BundleId assignedBundleId,
            IReadOnlyCollection<OutgoingMessage> outgoingMessages,
            MessageId? relatedToMessageId = null)
        {
            DocumentType = documentType;
            Receiver = receiver;
            DocumentReceiver = documentReceiver;
            BusinessReason = businessReason;
            SenderId = senderId;
            SenderRole = senderRole;
            AssignedBundleId = assignedBundleId;
            OutgoingMessages = outgoingMessages;
            RelatedToMessageId = relatedToMessageId;
        }

        public DocumentType DocumentType { get; }

        public string BusinessReason { get; }

        public ActorNumber SenderId { get; }

        public ActorRole SenderRole { get; }

        /// <summary>
        /// The actual receiver of the document.
        /// </summary>
        public Receiver Receiver { get; }

        /// <summary>
        /// The receiver written within the document.
        /// </summary>
        public Receiver DocumentReceiver { get; }

        public BundleId AssignedBundleId { get; private set; }

        public MessageId? RelatedToMessageId { get; private set; }

        public IReadOnlyCollection<OutgoingMessage> OutgoingMessages { get; set; }
    }
}
