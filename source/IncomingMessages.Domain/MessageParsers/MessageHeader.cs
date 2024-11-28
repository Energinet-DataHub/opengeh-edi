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

namespace Energinet.DataHub.EDI.IncomingMessages.Domain.MessageParsers;

public class MessageHeader(
    string messageId,
    string messageType,
    string businessReason,
    string senderId,
    string senderRole,
    string receiverId,
    string receiverRole,
    string createdAt,
    string? businessType = null)
{
    public string MessageId { get; } = messageId;

    public string MessageType { get; } = messageType;

    public string BusinessReason { get; } = businessReason;

    public string SenderId { get; } = senderId;

    public string SenderRole { get; } = senderRole;

    public string ReceiverId { get; } = receiverId;

    public string ReceiverRole { get; } = receiverRole;

    public string CreatedAt { get; } = createdAt;

    public string? BusinessType { get; } = businessType;
}
