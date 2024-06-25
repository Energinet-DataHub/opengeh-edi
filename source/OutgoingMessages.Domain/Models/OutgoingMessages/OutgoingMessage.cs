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

using System.Diagnostics.CodeAnalysis;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;

public class OutgoingMessage
{
    public static readonly FileStorageCategory FileStorageCategory = FileStorageCategory.OutgoingMessage();

    private string? _serializedContent;

    public OutgoingMessage(
        EventId eventId,
        DocumentType documentType,
        ActorNumber receiverId,
        Guid? processId,
        string businessReason,
        ActorRole receiverRole,
        ActorNumber senderId,
        ActorRole senderRole,
        string serializedContent,
        Instant createdAt,
        ProcessType messageCreatedFromProcess,
        MessageId? relatedToMessageId,
        string? gridAreaCode,
        ExternalId externalId,
        Guid? calculationId)
        : this(
            eventId,
            documentType,
            Receiver.Create(receiverId, receiverRole),
            Receiver.Create(receiverId, receiverRole),
            processId,
            businessReason,
            senderId,
            senderRole,
            serializedContent,
            createdAt,
            messageCreatedFromProcess,
            relatedToMessageId,
            gridAreaCode,
            externalId,
            calculationId)
    {
    }

    private OutgoingMessage(
        EventId eventId,
        DocumentType documentType,
        Receiver receiver,
        Receiver documentReceiver,
        Guid? processId,
        string businessReason,
        ActorNumber senderId,
        ActorRole senderRole,
        string serializedContent,
        Instant createdAt,
        ProcessType messageCreatedFromProcess,
        MessageId? relatedToMessageId,
        string? gridAreaCode,
        ExternalId externalId,
        Guid? calculationId)
    {
        Id = OutgoingMessageId.New();
        EventId = eventId;
        DocumentType = documentType;
        ProcessId = processId;
        BusinessReason = businessReason;
        SenderId = senderId;
        SenderRole = senderRole;
        MessageCreatedFromProcess = messageCreatedFromProcess;
        GridAreaCode = gridAreaCode;
        _serializedContent = serializedContent;
        RelatedToMessageId = relatedToMessageId;
        DocumentReceiver = documentReceiver;
        Receiver = receiver;
        CreatedAt = createdAt;
        FileStorageReference = CreateFileStorageReference(Receiver.Number, createdAt, Id);
        ExternalId = externalId;
        CalculationId = calculationId;
    }

    /// <summary>
    /// Should only be used by Entity Framework
    /// </summary>
    // ReSharper disable once UnusedMember.Local -- Used by Entity Framework
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private OutgoingMessage(
        DocumentType documentType,
        EventId eventId,
        Guid? processId,
        string businessReason,
        ActorNumber senderId,
        ActorRole senderRole,
        FileStorageReference fileStorageReference,
        ProcessType messageCreatedFromProcess,
        Instant createdAt,
        string? gridAreaCode,
        ExternalId externalId,
        Guid? calculationId)
    {
        Id = OutgoingMessageId.New();
        DocumentType = documentType;
        EventId = eventId;
        ProcessId = processId;
        BusinessReason = businessReason;
        SenderId = senderId;
        SenderRole = senderRole;
        FileStorageReference = fileStorageReference;
        MessageCreatedFromProcess = messageCreatedFromProcess;
        GridAreaCode = gridAreaCode;
        CreatedAt = createdAt;
        ExternalId = externalId;
        CalculationId = calculationId;
        // DocumentReceiver, EF will set this after the constructor
        // Receiver, EF will set this after the constructor
        // _serializedContent is set later in OutgoingMessageRepository, by getting the message from File Storage
    }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public OutgoingMessageId Id { get; }

    public DocumentType DocumentType { get; }

    public EventId EventId { get; }

    public Guid? ProcessId { get; }

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

    public Instant CreatedAt { get; private set; }

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

    public ExternalId ExternalId { get; }

    public Guid? CalculationId { get; }

    /// <summary>
    /// This method create a single outgoing message, for the receiver, based on the accepted energyResultMessage.
    /// </summary>
    public static OutgoingMessage CreateMessage(
        AcceptedEnergyResultMessageDto acceptedMessage,
        ISerializer serializer,
        Instant timestamp)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(acceptedMessage);

        return new OutgoingMessage(
            eventId: acceptedMessage.EventId,
            documentType: acceptedMessage.DocumentType,
            receiver: Receiver.Create(acceptedMessage.ReceiverNumber, acceptedMessage.ReceiverRole),
            documentReceiver: Receiver.Create(acceptedMessage.DocumentReceiverNumber, acceptedMessage.DocumentReceiverRole),
            processId: acceptedMessage.ProcessId,
            businessReason: acceptedMessage.BusinessReason,
            senderId: acceptedMessage.SenderId,
            senderRole: acceptedMessage.SenderRole,
            serializedContent: serializer.Serialize(acceptedMessage.Series),
            createdAt: timestamp,
            messageCreatedFromProcess: ProcessType.RequestEnergyResults,
            relatedToMessageId: acceptedMessage.RelatedToMessageId,
            gridAreaCode: acceptedMessage.Series.GridAreaCode,
            externalId: acceptedMessage.ExternalId,
            calculationId: null);
    }

    /// <summary>
    /// This method create a single outgoing message, for the receiver, based on the rejected energyResultMessage.
    /// </summary>
    public static OutgoingMessage CreateMessage(
        RejectedEnergyResultMessageDto rejectedMessage,
        ISerializer serializer,
        Instant timestamp)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(rejectedMessage);

        return new OutgoingMessage(
            eventId: rejectedMessage.EventId,
            documentType: rejectedMessage.DocumentType,
            receiver: Receiver.Create(rejectedMessage.ReceiverNumber, rejectedMessage.ReceiverRole),
            documentReceiver: Receiver.Create(rejectedMessage.DocumentReceiverNumber, rejectedMessage.DocumentReceiverRole),
            processId: rejectedMessage.ProcessId,
            businessReason: rejectedMessage.BusinessReason,
            senderId: rejectedMessage.SenderId,
            senderRole: rejectedMessage.SenderRole,
            serializedContent: serializer.Serialize(rejectedMessage.Series),
            createdAt: timestamp,
            messageCreatedFromProcess: ProcessType.RequestEnergyResults,
            relatedToMessageId: rejectedMessage.RelatedToMessageId,
            gridAreaCode: null,
            externalId: rejectedMessage.ExternalId,
            calculationId: null);
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
            energyResultMessage.EventId,
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
            energyResultMessage.Series.GridAreaCode,
            energyResultMessage.ExternalId,
            calculationId: energyResultMessage.CalculationId);
    }

    /// <summary>
    /// Create one outgoing message for the metered data responsible, based on the <paramref name="messageDto"/>.
    /// </summary>
    public static OutgoingMessage CreateMessage(
        EnergyResultPerGridAreaMessageDto messageDto,
        ISerializer serializer,
        Instant timestamp)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(messageDto);

        return new OutgoingMessage(
            messageDto.EventId,
            messageDto.DocumentType,
            messageDto.ReceiverNumber,
            messageDto.ProcessId,
            messageDto.BusinessReason,
            messageDto.ReceiverRole,
            messageDto.SenderId,
            messageDto.SenderRole,
            serializer.Serialize(messageDto.Series),
            timestamp,
            ProcessType.ReceiveEnergyResults,
            messageDto.RelatedToMessageId,
            messageDto.Series.GridAreaCode,
            messageDto.ExternalId,
            messageDto.CalculationId);
    }

    /// <summary>
    /// Create one outgoing message for the balance responsible, based on the <paramref name="messageDto"/>.
    /// </summary>
    public static OutgoingMessage CreateMessage(
        EnergyResultPerBalanceResponsibleMessageDto messageDto,
        ISerializer serializer,
        Instant timestamp)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(messageDto);

        return new OutgoingMessage(
            messageDto.EventId,
            messageDto.DocumentType,
            messageDto.ReceiverNumber,
            messageDto.ProcessId,
            messageDto.BusinessReason,
            messageDto.ReceiverRole,
            messageDto.SenderId,
            messageDto.SenderRole,
            serializer.Serialize(messageDto.Series),
            timestamp,
            ProcessType.ReceiveEnergyResults,
            messageDto.RelatedToMessageId,
            messageDto.Series.GridAreaCode,
            messageDto.ExternalId,
            messageDto.CalculationId);
    }

    /// <summary>
    /// Create two outgoing messages, one for the balance responsible and one for the energy supplier,
    /// based on the <paramref name="messageDto"/>.
    /// </summary>
    public static List<OutgoingMessage> CreateMessages(
        EnergyResultPerEnergySupplierPerBalanceResponsibleMessageDto messageDto,
        ISerializer serializer,
        Instant timestamp)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(messageDto);

        return
        [
            new OutgoingMessage(
                eventId: messageDto.EventId,
                documentType: messageDto.DocumentType,
                processId: messageDto.ProcessId,
                businessReason: messageDto.BusinessReason,
                receiverId: messageDto.EnergySupplierNumber,
                receiverRole: ActorRole.EnergySupplier,
                senderId: messageDto.SenderId,
                senderRole: messageDto.SenderRole,
                serializedContent: serializer.Serialize(messageDto.SeriesForEnergySupplier),
                createdAt: timestamp,
                messageCreatedFromProcess: ProcessType.ReceiveEnergyResults,
                relatedToMessageId: messageDto.RelatedToMessageId,
                gridAreaCode: messageDto.GridArea,
                externalId: messageDto.ExternalId,
                calculationId: messageDto.CalculationId),

            new OutgoingMessage(
                eventId: messageDto.EventId,
                documentType: messageDto.DocumentType,
                processId: messageDto.ProcessId,
                businessReason: messageDto.BusinessReason,
                receiverId: messageDto.BalanceResponsibleNumber,
                receiverRole: ActorRole.BalanceResponsibleParty,
                senderId: messageDto.SenderId,
                senderRole: messageDto.SenderRole,
                serializedContent: serializer.Serialize(messageDto.SeriesForBalanceResponsible),
                createdAt: timestamp,
                messageCreatedFromProcess: ProcessType.ReceiveEnergyResults,
                relatedToMessageId: messageDto.RelatedToMessageId,
                gridAreaCode: messageDto.GridArea,
                externalId: messageDto.ExternalId,
                calculationId: messageDto.CalculationId),
        ];
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
                wholesaleServicesMessageDto.EventId,
                wholesaleServicesMessageDto.DocumentType,
                wholesaleServicesMessageDto.ReceiverNumber,
                wholesaleServicesMessageDto.ProcessId,
                wholesaleServicesMessageDto.BusinessReason,
                wholesaleServicesMessageDto.ReceiverRole,
                wholesaleServicesMessageDto.SenderId,
                wholesaleServicesMessageDto.SenderRole,
                serializer.Serialize(wholesaleServicesMessageDto.Series),
                timestamp,
                ProcessType.ReceiveWholesaleResults,
                wholesaleServicesMessageDto.RelatedToMessageId,
                wholesaleServicesMessageDto.Series.GridAreaCode,
                wholesaleServicesMessageDto.ExternalId,
                calculationId: wholesaleServicesMessageDto.CalculationId),
            new(
                wholesaleServicesMessageDto.EventId,
                wholesaleServicesMessageDto.DocumentType,
                wholesaleServicesMessageDto.ChargeOwnerId,
                wholesaleServicesMessageDto.ProcessId,
                wholesaleServicesMessageDto.BusinessReason,
                GetChargeOwnerRole(wholesaleServicesMessageDto.ChargeOwnerId),
                wholesaleServicesMessageDto.SenderId,
                wholesaleServicesMessageDto.SenderRole,
                serializer.Serialize(wholesaleServicesMessageDto.Series),
                timestamp,
                ProcessType.ReceiveWholesaleResults,
                wholesaleServicesMessageDto.RelatedToMessageId,
                wholesaleServicesMessageDto.Series.GridAreaCode,
                wholesaleServicesMessageDto.ExternalId,
                calculationId: wholesaleServicesMessageDto.CalculationId),
        };
    }

    /// <summary>
    /// This method creates two outgoing messages, one for the receiver and one for the charge owner, based on the wholesaleResultMessage.
    /// </summary>
    public static IReadOnlyCollection<OutgoingMessage> CreateMessages(
        WholesaleAmountPerChargeDto wholesaleAmountPerChargeDto,
        ISerializer serializer,
        Instant timestamp)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(wholesaleAmountPerChargeDto);

        return new List<OutgoingMessage>()
        {
            new(
                wholesaleAmountPerChargeDto.EventId,
                wholesaleAmountPerChargeDto.DocumentType,
                wholesaleAmountPerChargeDto.EnergySupplierReceiverId,
                wholesaleAmountPerChargeDto.ProcessId,
                wholesaleAmountPerChargeDto.BusinessReason,
                ActorRole.EnergySupplier,
                senderId: DataHubDetails.DataHubActorNumber,
                senderRole: ActorRole.MeteredDataAdministrator,
                serializer.Serialize(wholesaleAmountPerChargeDto.Series),
                timestamp,
                ProcessType.ReceiveWholesaleResults,
                wholesaleAmountPerChargeDto.RelatedToMessageId,
                wholesaleAmountPerChargeDto.Series.GridAreaCode,
                wholesaleAmountPerChargeDto.ExternalId,
                wholesaleAmountPerChargeDto.CalculationId),
            new(
                wholesaleAmountPerChargeDto.EventId,
                wholesaleAmountPerChargeDto.DocumentType,
                wholesaleAmountPerChargeDto.ChargeOwnerReceiverId,
                wholesaleAmountPerChargeDto.ProcessId,
                wholesaleAmountPerChargeDto.BusinessReason,
                GetChargeOwnerRole(wholesaleAmountPerChargeDto.ChargeOwnerReceiverId),
                senderId: DataHubDetails.DataHubActorNumber,
                senderRole: ActorRole.MeteredDataAdministrator,
                serializer.Serialize(wholesaleAmountPerChargeDto.Series),
                timestamp,
                ProcessType.ReceiveWholesaleResults,
                wholesaleAmountPerChargeDto.RelatedToMessageId,
                wholesaleAmountPerChargeDto.Series.GridAreaCode,
                wholesaleAmountPerChargeDto.ExternalId,
                wholesaleAmountPerChargeDto.CalculationId),
        };
    }

    /// <summary>
    ///     This method create a single outgoing message, for the receiver, based on the rejected WholesaleServicesMessage.
    /// </summary>
    public static OutgoingMessage CreateMessage(
        RejectedWholesaleServicesMessageDto message,
        ISerializer serializer,
        Instant timestamp)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(message);

        return new OutgoingMessage(
            eventId: message.EventId,
            documentType: message.DocumentType,
            receiver: Receiver.Create(message.ReceiverNumber, message.ReceiverRole),
            documentReceiver: Receiver.Create(message.DocumentReceiverNumber, message.DocumentReceiverRole),
            processId: message.ProcessId,
            businessReason: message.BusinessReason,
            senderId: message.SenderId,
            senderRole: message.SenderRole,
            serializedContent: serializer.Serialize(message.Series),
            createdAt: timestamp,
            messageCreatedFromProcess: ProcessType.RequestWholesaleResults,
            relatedToMessageId: message.RelatedToMessageId,
            gridAreaCode: null,
            externalId: message.ExternalId,
            calculationId: null);
    }

    /// <summary>
    ///     This method create a single outgoing message, for the receiver, based on the accepted WholesaleServicesMessage.
    /// </summary>
    public static OutgoingMessage CreateMessage(
        AcceptedWholesaleServicesMessageDto message,
        ISerializer serializer,
        Instant timestamp)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(message);

        return new OutgoingMessage(
            eventId: message.EventId,
            documentType: message.DocumentType,
            receiver: Receiver.Create(message.ReceiverNumber, message.ReceiverRole),
            documentReceiver: Receiver.Create(message.DocumentReceiverNumber, message.DocumentReceiverRole),
            processId: message.ProcessId,
            businessReason: message.BusinessReason,
            senderId: message.SenderId,
            senderRole: message.SenderRole,
            serializedContent: serializer.Serialize(message.Series),
            createdAt: timestamp,
            messageCreatedFromProcess: ProcessType.RequestWholesaleResults,
            relatedToMessageId: message.RelatedToMessageId,
            gridAreaCode: message.Series.GridAreaCode,
            externalId: message.ExternalId,
            calculationId: null);
    }

    /// <summary>
    ///     This method create a single outgoing message, for the receiver, based on the WholesaleServicesTotalSumMessage.
    /// </summary>
    public static OutgoingMessage CreateMessage(
        WholesaleServicesTotalSumMessageDto wholesaleServicesTotalSumMessage,
        ISerializer serializer,
        Instant timestamp)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(wholesaleServicesTotalSumMessage);

        return new(
            wholesaleServicesTotalSumMessage.EventId,
            wholesaleServicesTotalSumMessage.DocumentType,
            wholesaleServicesTotalSumMessage.ReceiverNumber,
            wholesaleServicesTotalSumMessage.ProcessId,
            wholesaleServicesTotalSumMessage.BusinessReason,
            wholesaleServicesTotalSumMessage.ReceiverRole,
            wholesaleServicesTotalSumMessage.SenderId,
            wholesaleServicesTotalSumMessage.SenderRole,
            serializer.Serialize(wholesaleServicesTotalSumMessage.Series),
            timestamp,
            ProcessType.ReceiveWholesaleResults,
            wholesaleServicesTotalSumMessage.RelatedToMessageId,
            wholesaleServicesTotalSumMessage.Series.GridAreaCode,
            wholesaleServicesTotalSumMessage.ExternalId,
            calculationId: wholesaleServicesTotalSumMessage.CalculationId);
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
        if (Receiver != DocumentReceiver)
            throw new InvalidOperationException("Cannot delegate a message that has already been delegated to another actor");

        Receiver = delegatedToReceiver;
        FileStorageReference = CreateFileStorageReference(Receiver.Number, CreatedAt, Id);
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
        return chargeOwnerId == DataHubDetails.SystemOperatorActorNumber
            ? ActorRole.SystemOperator
            : ActorRole.GridOperator;
    }
}
