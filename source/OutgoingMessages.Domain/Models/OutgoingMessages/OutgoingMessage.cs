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

using System.Diagnostics.CodeAnalysis;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;
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
        string businessReason, // TODO: Make typed
        string serializedContent,
        Instant createdAt,
        ProcessType messageCreatedFromProcess,
        MessageId? relatedToMessageId,
        string? gridAreaCode,
        ExternalId externalId,
        Guid? calculationId,
        Instant? periodStartedAt,
        int dataCount,
        MeteringPointId? meteringPointId = null)
    {
        Id = OutgoingMessageId.New();
        EventId = eventId;
        DocumentType = documentType;
        ProcessId = processId;
        BusinessReason = businessReason;
        MessageCreatedFromProcess = messageCreatedFromProcess;
        GridAreaCode = gridAreaCode;
        _serializedContent = serializedContent;
        RelatedToMessageId = relatedToMessageId;
        Receiver = receiver;
        DocumentReceiver = documentReceiver;
        CreatedAt = createdAt;
        FileStorageReference = CreateFileStorageReference(Receiver.Number, createdAt, Id);
        ExternalId = externalId;
        CalculationId = calculationId;
        PeriodStartedAt = periodStartedAt;
        DataCount = dataCount;
        MeteringPointId = meteringPointId;
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
        Guid? calculationId,
        Instant? periodStartedAt,
        int dataCount)
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
        PeriodStartedAt = periodStartedAt;
        DataCount = dataCount;
        // DocumentReceiver, EF will set this after the constructor
        // Receiver, EF will set this after the constructor
        // _serializedContent is set later in OutgoingMessageRepository, by getting the message from File Storage
    }

    public OutgoingMessageId Id { get; }

    public DocumentType DocumentType { get; }

    public EventId EventId { get; }

    public Guid? ProcessId { get; }

    public string BusinessReason { get; }

    public ActorNumber SenderId { get; } = DataHubDetails.DataHubActorNumber;

    public ActorRole SenderRole { get; } = ActorRole.MeteredDataAdministrator;

    public string? GridAreaCode { get; }

    /// <summary>
    /// Is the Receiver of the message.
    /// The message will be place into the ActorMessageQueue of the Receiver.
    /// </summary>
    public Receiver Receiver { get; private set; }

    /// <summary>
    /// Is the Receiver written within the document.
    /// This will differ from the 'Receiver' if the message is delegated to another actor.
    /// Or when requesting energy or wholesale data, where the receiver is always the party who requested the data.
    /// </summary>
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
    /// In case a series is provided for the outgoing message, the start of the series period should be provided.
    /// It is used to determine if a actor already have received the message.
    /// </summary>
    public Instant? PeriodStartedAt { get; }

    /// <summary>
    /// The data count symbolizes the number of data points in the message. As an example, if the message contains a
    /// RSM-012 transaction, then the data count is the number of energy observations in the RSM-012 transaction.
    /// <remarks>
    /// The largest possible value should be an RSM-012 transaction for an entire year in quarter hourly resolution,
    /// which is 4 points pr. hour * 24 hours * 365 days = 35040 points in a year.
    /// </remarks>
    /// </summary>
    public int DataCount { get; }

    public MeteringPointId? MeteringPointId { get; }

    /// <summary>
    /// RowVersion is generated by the database and used for optimistic concurrency. Must be retrieved when loading
    /// the entity from the database, since Entity Framework uses it to throw an exception if the entity has been updated.
    /// </summary>
    /// <remarks>
    /// See https://learn.microsoft.com/en-us/ef/core/saving/concurrency?tabs=fluent-api
    /// </remarks>
    public byte[]? RowVersion { get; }

    public void AssignToBundle(BundleId bundleId)
    {
        if (AssignedBundleId != null)
            throw new InvalidOperationException($"Cannot assign a message to a bundle when it is already assigned to another bundle (MessageId={Id.Value}, CurrentBundleId={AssignedBundleId.Id}, NewBundleId={bundleId.Id})");

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

    private static FileStorageReference CreateFileStorageReference(ActorNumber receiverActorNumber, Instant timestamp, OutgoingMessageId outgoingMessageId)
    {
        return FileStorageReference.Create(FileStorageCategory, receiverActorNumber.Value, timestamp, outgoingMessageId.Value);
    }
}
