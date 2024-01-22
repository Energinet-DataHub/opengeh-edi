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
using System.Diagnostics.CodeAnalysis;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using NodaTime;
using NodaTime.Text;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing
{
    public class OutgoingMessage
    {
        private string _messageRecord;

        public OutgoingMessage(DocumentType documentType, ActorNumber receiverId, Guid processId, string businessReason, MarketRole receiverRole, ActorNumber senderId, MarketRole senderRole, string messageRecord, Instant timestamp)
        {
            Id = OutgoingMessageId.New();
            DocumentType = documentType;
            ReceiverId = receiverId;
            ProcessId = processId;
            BusinessReason = businessReason;
            ReceiverRole = receiverRole;
            SenderId = senderId;
            SenderRole = senderRole;
            _messageRecord = messageRecord;
            FileStorageReference = CreateFileStorageReference(Id, ReceiverId, timestamp);
        }

        // ReSharper disable once UnusedMember.Local -- Used by Entity Framework
        private OutgoingMessage(
            DocumentType documentType,
            ActorNumber receiverId,
            Guid processId,
            string businessReason,
            MarketRole receiverRole,
            ActorNumber senderId,
            MarketRole senderRole,
            FileStorageReference fileStorageReference)
        {
            Id = OutgoingMessageId.New();
            DocumentType = documentType;
            ReceiverId = receiverId;
            ProcessId = processId;
            BusinessReason = businessReason;
            ReceiverRole = receiverRole;
            SenderId = senderId;
            SenderRole = senderRole;
            FileStorageReference = fileStorageReference;

            _messageRecord = null!; // Message record is set later from FileStorage
        }

        public OutgoingMessageId Id { get; }

        public bool IsPublished { get; private set; }

        public ActorNumber ReceiverId { get; }

        public DocumentType DocumentType { get; }

        public Guid ProcessId { get; }

        public string BusinessReason { get; }

        public MarketRole ReceiverRole { get; }

        public ActorNumber SenderId { get; }

        public MarketRole SenderRole { get; }

        public Receiver Receiver => Receiver.Create(ReceiverId, ReceiverRole);

        public BundleId? AssignedBundleId { get; private set; }

        public FileStorageReference FileStorageReference { get; private set; }

        public void AssignToBundle(BundleId bundleId)
        {
            AssignedBundleId = bundleId;
        }

        public void SetMessageRecord(string messageRecord)
        {
            _messageRecord = messageRecord;
        }

        [SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Can cause error as a property because of serialization and message record maybe being null at the time")]
        public string GetMessageRecord()
        {
            return _messageRecord;
        }

        private static FileStorageReference CreateFileStorageReference(OutgoingMessageId id, ActorNumber receiverActorNumber, Instant timestamp)
        {
            var dateTimeUtc = timestamp.ToDateTimeUtc();

            var referenceString = $"{receiverActorNumber.Value}/{dateTimeUtc.Year:0000}/{dateTimeUtc.Month:00}/{dateTimeUtc.Day:00}/{id.Value:N}";

            return new FileStorageReference(referenceString);
        }
    }
}
