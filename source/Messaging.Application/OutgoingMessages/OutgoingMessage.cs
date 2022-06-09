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

using System;

namespace Messaging.Application.OutgoingMessages
{
    public class OutgoingMessage
    {
        public OutgoingMessage(string documentType, string recipientId, string correlationId, string originalMessageId, string processType, string receiverRole, string senderId, string senderRole, string marketActivityRecordPayload, string? reasonCode)
        {
            DocumentType = documentType;
            RecipientId = recipientId;
            CorrelationId = correlationId;
            OriginalMessageId = originalMessageId;
            ProcessType = processType;
            ReceiverRole = receiverRole;
            SenderId = senderId;
            SenderRole = senderRole;
            MarketActivityRecordPayload = marketActivityRecordPayload;
            ReasonCode = reasonCode;
            Id = Guid.NewGuid();
        }

        private OutgoingMessage(Guid id, string documentType, string recipientId, string correlationId, string originalMessageId, string processType, string receiverRole, string senderId, string senderRole, string marketActivityRecordPayload, string? reasonCode)
        {
            DocumentType = documentType;
            RecipientId = recipientId;
            CorrelationId = correlationId;
            OriginalMessageId = originalMessageId;
            ProcessType = processType;
            ReceiverRole = receiverRole;
            SenderId = senderId;
            SenderRole = senderRole;
            MarketActivityRecordPayload = marketActivityRecordPayload;
            ReasonCode = reasonCode;
            Id = id;
        }

        public Guid Id { get; }

        public bool IsPublished { get; private set; }

        public string RecipientId { get; }

        public string DocumentType { get; }

        public string? ReasonCode { get; }

        public string CorrelationId { get; }

        public string OriginalMessageId { get; }

        public string ProcessType { get; }

        public string ReceiverRole { get; }

        public string SenderId { get; }

        public string SenderRole { get; }

        public string MarketActivityRecordPayload { get; }

        public void Published()
        {
            IsPublished = true;
        }
    }
}
