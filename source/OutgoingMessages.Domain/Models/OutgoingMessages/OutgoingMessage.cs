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
        Receiver = receiver;
        // We only cases when requesting making sure the receiver written in the document is not the same as the receiver of the message.
        DocumentReceiver = documentReceiver;
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
        OutgoingMessageId id,
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
        Id = id;
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
}
