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

namespace Energinet.DataHub.EDI.Common
{
    public class MessageHeader
    {
        public MessageHeader(string messageId, string messageType, string businessReason, string senderId, string senderRole, string receiverId, string receiverRole, string createdAt, string? businessType = null, string? authenticatedUser = null, string? authenticatedUserRole = null)
        {
            MessageId = messageId;
            MessageType = messageType;
            BusinessReason = businessReason;
            SenderId = senderId;
            SenderRole = senderRole;
            ReceiverId = receiverId;
            ReceiverRole = receiverRole;
            CreatedAt = createdAt;
            BusinessType = businessType;
            AuthenticatedUser = authenticatedUser;
            AuthenticatedUserRole = authenticatedUserRole;
        }

        public string MessageId { get; }

        public string MessageType { get; }

        public string BusinessReason { get; }

        public string SenderId { get; }

        public string SenderRole { get; }

        public string ReceiverId { get; }

        public string ReceiverRole { get; }

        public string CreatedAt { get; }

        public string? BusinessType { get; }

        //Todo: temp solution until messageReceiver doesn't depend on authenticated user
        public string? AuthenticatedUser { get; set; }

        // Todo: temp solution until messageReceiver doesn't depend on authenticated user
        public string? AuthenticatedUserRole { get; set; }
    }
}
