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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Common.Serialization;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing
{
    public class OutgoingMessage
    {
        public static readonly FileStorageCategory FileStorageCategory = FileStorageCategory.OutgoingMessage();

        private string? _serializedContent;

        public OutgoingMessage(
            DocumentType documentType,
            ActorNumber receiverId,
            Guid processId,
            string businessReason,
            ActorRole receiverRole,
            ActorNumber senderId,
            ActorRole senderRole,
            string serializedContent,
            Instant timestamp,
            MessageId? relatedToMessageId = null)
        {
            Id = OutgoingMessageId.New();
            DocumentType = documentType;
            ReceiverId = receiverId;
            ProcessId = processId;
            BusinessReason = businessReason;
            ReceiverRole = receiverRole;
            SenderId = senderId;
            SenderRole = senderRole;
            _serializedContent = serializedContent;
            FileStorageReference = CreateFileStorageReference(ReceiverId, timestamp, Id);
            RelatedToMessageId = relatedToMessageId;
        }

        /// <summary>
        /// Should only be used by Entity Framework
        /// </summary>
        // ReSharper disable once UnusedMember.Local -- Used by Entity Framework
        private OutgoingMessage(
            DocumentType documentType,
            ActorNumber receiverId,
            Guid processId,
            string businessReason,
            ActorRole receiverRole,
            ActorNumber senderId,
            ActorRole senderRole,
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
            // _serializedContent is set later in OutgoingMessageRepository, by getting the message from File Storage
        }

        public OutgoingMessageId Id { get; }

        public bool IsPublished { get; private set; }

        public ActorNumber ReceiverId { get; }

        public DocumentType DocumentType { get; }

        public Guid ProcessId { get; }

        public string BusinessReason { get; }

        public ActorRole ReceiverRole { get; }

        public ActorNumber SenderId { get; }

        public ActorRole SenderRole { get; }

        public Receiver Receiver => Receiver.Create(ReceiverId, ReceiverRole);

        public BundleId? AssignedBundleId { get; private set; }

        public FileStorageReference FileStorageReference { get; private set; }

        /// <summary>
        /// If this attribute has a value, then it is used to store the message id of a request from an actor.
        /// Giving us the possibility to track the request and the response.
        /// </summary>
        public MessageId? RelatedToMessageId { get; set; }

        /// <summary>
        /// This method create a single outgoing message, for the receiver, based on the accepted energyResultMessage.
        /// </summary>
        /// <param name="acceptedEnergyResultMessage"></param>
        /// <param name="timestamp"></param>
        public static OutgoingMessage CreateMessage(
            AcceptedEnergyResultMessageDto acceptedEnergyResultMessage,
            Instant timestamp)
        {
            ArgumentNullException.ThrowIfNull(acceptedEnergyResultMessage);
            return new OutgoingMessage(
                acceptedEnergyResultMessage.DocumentType,
                acceptedEnergyResultMessage.ReceiverId,
                acceptedEnergyResultMessage.ProcessId,
                acceptedEnergyResultMessage.BusinessReason,
                acceptedEnergyResultMessage.ReceiverRole,
                acceptedEnergyResultMessage.SenderId,
                acceptedEnergyResultMessage.SenderRole,
                new Serializer().Serialize(acceptedEnergyResultMessage.Series),
                timestamp,
                acceptedEnergyResultMessage.RelatedToMessageId);
        }

        /// <summary>
        /// This method create a single outgoing message, for the receiver, based on the rejected energyResultMessage.
        /// </summary>
        /// <param name="rejectedEnergyResultMessage"></param>
        /// <param name="timestamp"></param>
        public static OutgoingMessage CreateMessage(
            RejectedEnergyResultMessageDto rejectedEnergyResultMessage,
            Instant timestamp)
        {
            ArgumentNullException.ThrowIfNull(rejectedEnergyResultMessage);
            return new OutgoingMessage(
                rejectedEnergyResultMessage.DocumentType,
                rejectedEnergyResultMessage.ReceiverId,
                rejectedEnergyResultMessage.ProcessId,
                rejectedEnergyResultMessage.BusinessReason,
                rejectedEnergyResultMessage.ReceiverRole,
                rejectedEnergyResultMessage.SenderId,
                rejectedEnergyResultMessage.SenderRole,
                new Serializer().Serialize(rejectedEnergyResultMessage.Series),
                timestamp,
                rejectedEnergyResultMessage.RelatedToMessageId);
        }

        /// <summary>
        /// This method create a single outgoing message, for the receiver, based on the energyResultMessage.
        /// </summary>
        /// <param name="energyResultMessage"></param>
        /// <param name="timestamp"></param>
        public static OutgoingMessage CreateMessage(
            EnergyResultMessageDto energyResultMessage,
            Instant timestamp)
        {
            ArgumentNullException.ThrowIfNull(energyResultMessage);
            return new OutgoingMessage(
                energyResultMessage.DocumentType,
                energyResultMessage.ReceiverId,
                energyResultMessage.ProcessId,
                energyResultMessage.BusinessReason,
                energyResultMessage.ReceiverRole,
                energyResultMessage.SenderId,
                energyResultMessage.SenderRole,
                new Serializer().Serialize(energyResultMessage.Series),
                timestamp,
                energyResultMessage.RelatedToMessageId);
        }

        /// <summary>
        /// This method creates two outgoing messages, one for the receiver and one for the charge owner, based on the wholesaleResultMessage.
        /// </summary>
        /// <param name="wholesaleServicesMessageDto"></param>
        /// <param name="timestamp"></param>
        public static IReadOnlyCollection<OutgoingMessage> CreateMessages(
            WholesaleServicesMessageDto wholesaleServicesMessageDto,
            Instant timestamp)
        {
            ArgumentNullException.ThrowIfNull(wholesaleServicesMessageDto);
            return new List<OutgoingMessage>()
            {
                new(
                    wholesaleServicesMessageDto.DocumentType,
                    wholesaleServicesMessageDto.ReceiverId,
                    wholesaleServicesMessageDto.ProcessId,
                    wholesaleServicesMessageDto.BusinessReason,
                    wholesaleServicesMessageDto.ReceiverRole,
                    wholesaleServicesMessageDto.SenderId,
                    wholesaleServicesMessageDto.SenderRole,
                    new Serializer().Serialize(wholesaleServicesMessageDto.Series),
                    timestamp,
                    wholesaleServicesMessageDto.RelatedToMessageId),
                new(
                    wholesaleServicesMessageDto.DocumentType,
                    wholesaleServicesMessageDto.ChargeOwnerId,
                    wholesaleServicesMessageDto.ProcessId,
                    wholesaleServicesMessageDto.BusinessReason,
                    GetChargeOwnerRole(wholesaleServicesMessageDto.ChargeOwnerId),
                    wholesaleServicesMessageDto.SenderId,
                    wholesaleServicesMessageDto.SenderRole,
                    new Serializer().Serialize(wholesaleServicesMessageDto.Series),
                    timestamp,
                    wholesaleServicesMessageDto.RelatedToMessageId),
            };
        }

        public void AssignToBundle(BundleId bundleId)
        {
            AssignedBundleId = bundleId;
        }

        public void SetSerializedContent(string serializedMessageContent)
        {
            _serializedContent = serializedMessageContent;
        }

        [SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Can cause error as a property because of serialization and message record maybe being null at the time")]
        public string GetSerializedContent()
        {
            if (_serializedContent == null)
                throw new InvalidOperationException($"{nameof(OutgoingMessage)}.{nameof(_serializedContent)} is null which shouldn't be possible. Make sure the {nameof(OutgoingMessage)} is retrieved by a {nameof(IOutgoingMessageRepository)}, which sets the {nameof(_serializedContent)} field");

            return _serializedContent;
        }

        private static FileStorageReference CreateFileStorageReference(ActorNumber receiverActorNumber, Instant timestamp, OutgoingMessageId outgoingMessageId)
        {
            return FileStorageReference.Create(FileStorageCategory, receiverActorNumber.Value, timestamp, outgoingMessageId.Value);
        }

        private static ActorRole GetChargeOwnerRole(ActorNumber chargeOwnerId)
        {
            if (chargeOwnerId == DataHubDetails.DataHubActorNumber)
            {
                return ActorRole.SystemOperator;
            }

            return ActorRole.GridOperator;
        }
    }
}
