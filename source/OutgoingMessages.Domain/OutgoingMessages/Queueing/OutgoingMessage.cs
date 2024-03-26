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
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
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
            ProcessType messageCreatedFromProcess,
            MessageId? relatedToMessageId = null,
            string? gridAreaCode = null)
        {
            ArgumentNullException.ThrowIfNull(receiverId);
            Id = OutgoingMessageId.New();
            DocumentType = documentType;
            ProcessId = processId;
            BusinessReason = businessReason;
            SenderId = senderId;
            SenderRole = senderRole;
            MessageCreatedFromProcess = messageCreatedFromProcess;
            GridAreaCode = gridAreaCode;
            _serializedContent = serializedContent;
            RelatedToMessageId = relatedToMessageId;
            DocumentReceiver = Receiver.Create(receiverId, receiverRole);
            Receiver = Receiver.Create(receiverId, receiverRole);
            FileStorageReference = CreateFileStorageReference(Receiver.Number, timestamp, Id);
        }

        /// <summary>
        /// Should only be used by Entity Framework
        /// </summary>
        // ReSharper disable once UnusedMember.Local -- Used by Entity Framework
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private OutgoingMessage(
            DocumentType documentType,
            Guid processId,
            string businessReason,
            ActorNumber senderId,
            ActorRole senderRole,
            FileStorageReference fileStorageReference,
            ProcessType messageCreatedFromProcess,
            string? gridAreaCode)
        {
            Id = OutgoingMessageId.New();
            DocumentType = documentType;
            ProcessId = processId;
            BusinessReason = businessReason;
            SenderId = senderId;
            SenderRole = senderRole;
            FileStorageReference = fileStorageReference;
            MessageCreatedFromProcess = messageCreatedFromProcess;
            GridAreaCode = gridAreaCode;
            // DocumentReceiver, EF will set this after the constructor
            // Receiver, EF will set this after the constructor
            // _serializedContent is set later in OutgoingMessageRepository, by getting the message from File Storage
        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public OutgoingMessageId Id { get; }

        public DocumentType DocumentType { get; }

        public Guid ProcessId { get; }

        public string BusinessReason { get; }

        public ActorNumber SenderId { get; }

        public ActorRole SenderRole { get; }

        public string? GridAreaCode { get; }

        /// <summary>
        /// Reference the actor queue that should receive the message.
        /// </summary>
        public Receiver Receiver { get; private set; }

        public Receiver DocumentReceiver { get; }

        public BundleId? AssignedBundleId { get; private set; }

        public FileStorageReference FileStorageReference { get; private set; }

        /// <summary>
        /// Describes the process type that the message was created from.
        /// </summary>
        public ProcessType MessageCreatedFromProcess { get; }

        /// <summary>
        /// If this attribute has a value, then it is used to store the message id of a request from an actor.
        /// Giving us the possibility to track the request and the response.
        /// </summary>
        public MessageId? RelatedToMessageId { get; set; }

        /// <summary>
        /// This method create a single outgoing message, for the receiver, based on the accepted energyResultMessage.
        /// </summary>
        public static OutgoingMessage CreateMessage(
            AcceptedEnergyResultMessageDto acceptedEnergyResultMessage,
            ISerializer serializer,
            Instant timestamp)
        {
            ArgumentNullException.ThrowIfNull(serializer);
            ArgumentNullException.ThrowIfNull(acceptedEnergyResultMessage);

            return new OutgoingMessage(
                acceptedEnergyResultMessage.DocumentType,
                acceptedEnergyResultMessage.ReceiverNumber,
                acceptedEnergyResultMessage.ProcessId,
                acceptedEnergyResultMessage.BusinessReason,
                acceptedEnergyResultMessage.ReceiverRole,
                acceptedEnergyResultMessage.SenderId,
                acceptedEnergyResultMessage.SenderRole,
                serializer.Serialize(acceptedEnergyResultMessage.Series),
                timestamp,
                ProcessType.RequestEnergyResults,
                acceptedEnergyResultMessage.RelatedToMessageId);
        }

        /// <summary>
        /// This method create a single outgoing message, for the receiver, based on the rejected energyResultMessage.
        /// </summary>
        public static OutgoingMessage CreateMessage(
            RejectedEnergyResultMessageDto rejectedEnergyResultMessage,
            ISerializer serializer,
            Instant timestamp)
        {
            ArgumentNullException.ThrowIfNull(serializer);
            ArgumentNullException.ThrowIfNull(rejectedEnergyResultMessage);

            return new OutgoingMessage(
                rejectedEnergyResultMessage.DocumentType,
                rejectedEnergyResultMessage.ReceiverNumber,
                rejectedEnergyResultMessage.ProcessId,
                rejectedEnergyResultMessage.BusinessReason,
                rejectedEnergyResultMessage.ReceiverRole,
                rejectedEnergyResultMessage.SenderId,
                rejectedEnergyResultMessage.SenderRole,
                serializer.Serialize(rejectedEnergyResultMessage.Series),
                timestamp,
                ProcessType.RequestEnergyResults,
                relatedToMessageId: rejectedEnergyResultMessage.RelatedToMessageId);
        }

        /// <summary>
        /// This method create a single outgoing message, for the receiver, based on the energyResultMessage.
        /// </summary>
        public static OutgoingMessage CreateMessage(
            EnergyResultMessageDto energyResultMessage,
            ISerializer serializer,
            Instant timestamp)
        {
            ArgumentNullException.ThrowIfNull(serializer);
            ArgumentNullException.ThrowIfNull(energyResultMessage);

            return new OutgoingMessage(
                energyResultMessage.DocumentType,
                energyResultMessage.ReceiverNumber,
                energyResultMessage.ProcessId,
                energyResultMessage.BusinessReason,
                energyResultMessage.ReceiverRole,
                energyResultMessage.SenderId,
                energyResultMessage.SenderRole,
                serializer.Serialize(energyResultMessage.Series),
                timestamp,
                ProcessType.ReceiveEnergyResults,
                energyResultMessage.RelatedToMessageId,
                energyResultMessage.Series.GridAreaCode);
        }

        /// <summary>
        /// This method creates two outgoing messages, one for the receiver and one for the charge owner, based on the wholesaleResultMessage.
        /// </summary>
        public static IReadOnlyCollection<OutgoingMessage> CreateMessages(
            WholesaleServicesMessageDto wholesaleServicesMessageDto,
            ISerializer serializer,
            Instant timestamp)
        {
            ArgumentNullException.ThrowIfNull(serializer);
            ArgumentNullException.ThrowIfNull(wholesaleServicesMessageDto);

            return new List<OutgoingMessage>()
            {
                new(
                    wholesaleServicesMessageDto.DocumentType,
                    wholesaleServicesMessageDto.ReceiverNumber,
                    wholesaleServicesMessageDto.ProcessId,
                    wholesaleServicesMessageDto.BusinessReason,
                    wholesaleServicesMessageDto.ReceiverRole,
                    wholesaleServicesMessageDto.SenderId,
                    wholesaleServicesMessageDto.SenderRole,
                    serializer.Serialize(wholesaleServicesMessageDto.Series),
                    timestamp,
                    ProcessType.RequestWholesaleResults,
                    wholesaleServicesMessageDto.RelatedToMessageId,
                    wholesaleServicesMessageDto.Series.GridAreaCode),
                new(
                    wholesaleServicesMessageDto.DocumentType,
                    wholesaleServicesMessageDto.ChargeOwnerId,
                    wholesaleServicesMessageDto.ProcessId,
                    wholesaleServicesMessageDto.BusinessReason,
                    GetChargeOwnerRole(wholesaleServicesMessageDto.ChargeOwnerId),
                    wholesaleServicesMessageDto.SenderId,
                    wholesaleServicesMessageDto.SenderRole,
                    serializer.Serialize(wholesaleServicesMessageDto.Series),
                    timestamp,
                    ProcessType.RequestWholesaleResults,
                    wholesaleServicesMessageDto.RelatedToMessageId,
                    wholesaleServicesMessageDto.Series.GridAreaCode),
            };
        }

        /// <summary>
        ///     This method create a single outgoing message, for the receiver, based on the rejected WholesaleServicesMessage.
        /// </summary>
        public static OutgoingMessage CreateMessage(
            RejectedWholesaleServicesMessageDto rejectedWholesaleServicesMessage,
            ISerializer serializer,
            Instant timestamp)
        {
            ArgumentNullException.ThrowIfNull(serializer);
            ArgumentNullException.ThrowIfNull(rejectedWholesaleServicesMessage);

            return new OutgoingMessage(
                rejectedWholesaleServicesMessage.DocumentType,
                rejectedWholesaleServicesMessage.ReceiverNumber,
                rejectedWholesaleServicesMessage.ProcessId,
                rejectedWholesaleServicesMessage.BusinessReason,
                rejectedWholesaleServicesMessage.ReceiverRole,
                rejectedWholesaleServicesMessage.SenderId,
                rejectedWholesaleServicesMessage.SenderRole,
                serializer.Serialize(rejectedWholesaleServicesMessage.Series),
                timestamp,
                ProcessType.RequestWholesaleResults,
                rejectedWholesaleServicesMessage.RelatedToMessageId);
        }

        /// <summary>
        ///     This method create a single outgoing message, for the receiver, based on the accepted WholesaleServicesMessage.
        /// </summary>
        public static OutgoingMessage CreateMessage(AcceptedWholesaleServicesMessageDto acceptedWholesaleServicesMessage, ISerializer serializer, Instant timestamp)
        {
            ArgumentNullException.ThrowIfNull(serializer);
            ArgumentNullException.ThrowIfNull(acceptedWholesaleServicesMessage);

            return new OutgoingMessage(
                acceptedWholesaleServicesMessage.DocumentType,
                acceptedWholesaleServicesMessage.ReceiverNumber,
                acceptedWholesaleServicesMessage.ProcessId,
                acceptedWholesaleServicesMessage.BusinessReason,
                acceptedWholesaleServicesMessage.ReceiverRole,
                acceptedWholesaleServicesMessage.SenderId,
                acceptedWholesaleServicesMessage.SenderRole,
                serializer.Serialize(acceptedWholesaleServicesMessage.Series),
                timestamp,
                ProcessType.ReceiveWholesaleResults,
                acceptedWholesaleServicesMessage.RelatedToMessageId);
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

        /// <summary>
        /// The ActorMessageQueue metadata (which ActorMessageQueue the OutgoingMessage should be saved in).
        /// This is implemented to support the "hack" where a NotifyAggregatedMeasureData document for a MeteredDataResponsible
        /// should be added to the GridOperator queue
        /// </summary>
        public Receiver GetActorMessageQueueMetadata()
        {
            var actorMessageQueueReceiverRole = Receiver.ActorRole;

            if (WorkaroundFlags.MeteredDataResponsibleToGridOperatorHack)
            {
                // AggregatedMeasureData messages (notify & reject) to the MDR role should always be added to the GridOperator queue
                if (DocumentIsAggregatedMeasureData(DocumentType))
                    actorMessageQueueReceiverRole = actorMessageQueueReceiverRole.ForActorMessageQueue();
            }

            return Receiver.Create(Receiver.Number, actorMessageQueueReceiverRole);
        }

        /// <summary>
        /// Override the current receiver of the message when the message is delegated to another actor.
        /// </summary>
        public void DelegateTo(Receiver delegatedToReceiver)
        {
            Receiver = delegatedToReceiver;
        }

        private static bool DocumentIsAggregatedMeasureData(DocumentType documentType)
        {
            return documentType == DocumentType.NotifyAggregatedMeasureData || documentType == DocumentType.RejectRequestAggregatedMeasureData;
        }

        private static FileStorageReference CreateFileStorageReference(ActorNumber receiverActorNumber, Instant timestamp, OutgoingMessageId outgoingMessageId)
        {
            return FileStorageReference.Create(FileStorageCategory, receiverActorNumber.Value, timestamp, outgoingMessageId.Value);
        }

        private static ActorRole GetChargeOwnerRole(ActorNumber chargeOwnerId)
        {
            return chargeOwnerId == DataHubDetails.DataHubActorNumber
                ? ActorRole.SystemOperator
                : ActorRole.GridOperator;
        }
    }
}
