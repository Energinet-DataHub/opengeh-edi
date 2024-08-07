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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.EnergyResultMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.EnergyResultMessages.Request;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.WholesaleResultMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.WholesaleResultMessages.Request;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application;

public static class OutgoingMessageFactory
{
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
            calculationId: null,
            documentReceiver: Receiver.Create(acceptedMessage.DocumentReceiverNumber, acceptedMessage.DocumentReceiverRole));
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
            calculationId: null,
            documentReceiver: Receiver.Create(rejectedMessage.DocumentReceiverNumber, rejectedMessage.DocumentReceiverRole));
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
            Receiver.Create(messageDto.ReceiverNumber, messageDto.ReceiverRole),
            Receiver.Create(messageDto.ReceiverNumber, messageDto.ReceiverRole),
            messageDto.ProcessId,
            messageDto.BusinessReason,
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
            Receiver.Create(messageDto.ReceiverNumber, messageDto.ReceiverRole),
            Receiver.Create(messageDto.ReceiverNumber, messageDto.ReceiverRole),
            messageDto.ProcessId,
            messageDto.BusinessReason,
            messageDto.SenderId,
            messageDto.ReceiverRole,
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

        List<OutgoingMessage> outgoingMessages = [
            new OutgoingMessage(
                eventId: messageDto.EventId,
                documentType: messageDto.DocumentType,
                processId: messageDto.ProcessId,
                businessReason: messageDto.BusinessReason,
                receiver: Receiver.Create(messageDto.EnergySupplierNumber, ActorRole.EnergySupplier),
                documentReceiver: Receiver.Create(messageDto.BalanceResponsibleNumber, ActorRole.EnergySupplier),
                senderId: messageDto.SenderId,
                senderRole: messageDto.SenderRole,
                serializedContent: serializer.Serialize(messageDto.SeriesForEnergySupplier),
                createdAt: timestamp,
                messageCreatedFromProcess: ProcessType.ReceiveEnergyResults,
                relatedToMessageId: messageDto.RelatedToMessageId,
                gridAreaCode: messageDto.GridArea,
                externalId: messageDto.ExternalId,
                calculationId: messageDto.CalculationId),
        ];

        // Only create a message for the balance responsible if the business reason is BalanceFixing or PreliminaryAggregation
        if (messageDto.BusinessReason is not DataHubNames.BusinessReason.WholesaleFixing &&
            messageDto.BusinessReason is not DataHubNames.BusinessReason.Correction)
        {
            var outgoingMessageToBalanceResponsible = new OutgoingMessage(
                eventId: messageDto.EventId,
                documentType: messageDto.DocumentType,
                processId: messageDto.ProcessId,
                businessReason: messageDto.BusinessReason,
                receiver: Receiver.Create(messageDto.BalanceResponsibleNumber, ActorRole.BalanceResponsibleParty),
                documentReceiver: Receiver.Create(messageDto.BalanceResponsibleNumber, ActorRole.BalanceResponsibleParty),
                senderId: messageDto.SenderId,
                senderRole: messageDto.SenderRole,
                serializedContent: serializer.Serialize(messageDto.SeriesForBalanceResponsible),
                createdAt: timestamp,
                messageCreatedFromProcess: ProcessType.ReceiveEnergyResults,
                relatedToMessageId: messageDto.RelatedToMessageId,
                gridAreaCode: messageDto.GridArea,
                externalId: messageDto.ExternalId,
                calculationId: messageDto.CalculationId);

            outgoingMessages.Add(outgoingMessageToBalanceResponsible);
        }

        return outgoingMessages;
    }

    /// <summary>
    /// This method creates two outgoing messages, one for the receiver and one for the charge owner, based on the wholesaleResultMessage.
    /// </summary>
    public static IReadOnlyCollection<OutgoingMessage> CreateMessages(
        WholesaleAmountPerChargeMessageDto wholesaleAmountPerChargeMessageDto,
        ISerializer serializer,
        Instant timestamp)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(wholesaleAmountPerChargeMessageDto);

        return new List<OutgoingMessage>()
        {
            new(
                wholesaleAmountPerChargeMessageDto.EventId,
                wholesaleAmountPerChargeMessageDto.DocumentType,
                Receiver.Create(wholesaleAmountPerChargeMessageDto.EnergySupplierReceiverId, ActorRole.EnergySupplier),
                Receiver.Create(wholesaleAmountPerChargeMessageDto.EnergySupplierReceiverId, ActorRole.EnergySupplier),
                wholesaleAmountPerChargeMessageDto.ProcessId,
                wholesaleAmountPerChargeMessageDto.BusinessReason,
                senderId: wholesaleAmountPerChargeMessageDto.SenderId,
                senderRole: wholesaleAmountPerChargeMessageDto.SenderRole,
                serializer.Serialize(wholesaleAmountPerChargeMessageDto.Series),
                timestamp,
                ProcessType.ReceiveWholesaleResults,
                wholesaleAmountPerChargeMessageDto.RelatedToMessageId,
                wholesaleAmountPerChargeMessageDto.Series.GridAreaCode,
                wholesaleAmountPerChargeMessageDto.ExternalId,
                wholesaleAmountPerChargeMessageDto.CalculationId),
            new(
                wholesaleAmountPerChargeMessageDto.EventId,
                wholesaleAmountPerChargeMessageDto.DocumentType,
                Receiver.Create(
                    wholesaleAmountPerChargeMessageDto.ChargeOwnerReceiverId,
                    GetChargeOwnerRole(wholesaleAmountPerChargeMessageDto.ChargeOwnerReceiverId)),
                Receiver.Create(
                    wholesaleAmountPerChargeMessageDto.ChargeOwnerReceiverId,
                    GetChargeOwnerRole(wholesaleAmountPerChargeMessageDto.ChargeOwnerReceiverId)),
                wholesaleAmountPerChargeMessageDto.ProcessId,
                wholesaleAmountPerChargeMessageDto.BusinessReason,
                senderId: wholesaleAmountPerChargeMessageDto.SenderId,
                senderRole: wholesaleAmountPerChargeMessageDto.SenderRole,
                serializer.Serialize(wholesaleAmountPerChargeMessageDto.Series),
                timestamp,
                ProcessType.ReceiveWholesaleResults,
                wholesaleAmountPerChargeMessageDto.RelatedToMessageId,
                wholesaleAmountPerChargeMessageDto.Series.GridAreaCode,
                wholesaleAmountPerChargeMessageDto.ExternalId,
                wholesaleAmountPerChargeMessageDto.CalculationId),
        };
    }

    /// <summary>
    /// This method creates two outgoing messages, one for the receiver and one for the charge owner, based on the wholesaleResultMessage.
    /// </summary>
    public static IReadOnlyCollection<OutgoingMessage> CreateMessages(
        WholesaleMonthlyAmountPerChargeMessageDto wholesaleMonthlyAmountPerChargeMessageDto,
        ISerializer serializer,
        Instant timestamp)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(wholesaleMonthlyAmountPerChargeMessageDto);

        return new List<OutgoingMessage>
        {
            new(
                wholesaleMonthlyAmountPerChargeMessageDto.EventId,
                wholesaleMonthlyAmountPerChargeMessageDto.DocumentType,
                Receiver.Create(wholesaleMonthlyAmountPerChargeMessageDto.EnergySupplierReceiverId, ActorRole.EnergySupplier),
                Receiver.Create(wholesaleMonthlyAmountPerChargeMessageDto.EnergySupplierReceiverId, ActorRole.EnergySupplier),
                wholesaleMonthlyAmountPerChargeMessageDto.ProcessId,
                wholesaleMonthlyAmountPerChargeMessageDto.BusinessReason,
                senderId: wholesaleMonthlyAmountPerChargeMessageDto.SenderId,
                senderRole: wholesaleMonthlyAmountPerChargeMessageDto.SenderRole,
                serializer.Serialize(wholesaleMonthlyAmountPerChargeMessageDto.Series),
                timestamp,
                ProcessType.ReceiveWholesaleResults,
                wholesaleMonthlyAmountPerChargeMessageDto.RelatedToMessageId,
                wholesaleMonthlyAmountPerChargeMessageDto.Series.GridAreaCode,
                wholesaleMonthlyAmountPerChargeMessageDto.ExternalId,
                wholesaleMonthlyAmountPerChargeMessageDto.CalculationId),
            new(
                wholesaleMonthlyAmountPerChargeMessageDto.EventId,
                wholesaleMonthlyAmountPerChargeMessageDto.DocumentType,
                Receiver.Create(
                    wholesaleMonthlyAmountPerChargeMessageDto.ChargeOwnerReceiverId,
                    GetChargeOwnerRole(wholesaleMonthlyAmountPerChargeMessageDto.ChargeOwnerReceiverId)),
                Receiver.Create(
                    wholesaleMonthlyAmountPerChargeMessageDto.ChargeOwnerReceiverId,
                    GetChargeOwnerRole(wholesaleMonthlyAmountPerChargeMessageDto.ChargeOwnerReceiverId)),
                wholesaleMonthlyAmountPerChargeMessageDto.ProcessId,
                wholesaleMonthlyAmountPerChargeMessageDto.BusinessReason,
                senderId: wholesaleMonthlyAmountPerChargeMessageDto.SenderId,
                senderRole: wholesaleMonthlyAmountPerChargeMessageDto.SenderRole,
                serializer.Serialize(wholesaleMonthlyAmountPerChargeMessageDto.Series),
                timestamp,
                ProcessType.ReceiveWholesaleResults,
                wholesaleMonthlyAmountPerChargeMessageDto.RelatedToMessageId,
                wholesaleMonthlyAmountPerChargeMessageDto.Series.GridAreaCode,
                wholesaleMonthlyAmountPerChargeMessageDto.ExternalId,
                wholesaleMonthlyAmountPerChargeMessageDto.CalculationId),
        };
    }

    /// <summary>
    /// This method creates an outgoing message, one for the receiver based on the WholesaleTotalAmountMessageDto.
    /// </summary>
    public static OutgoingMessage CreateMessage(WholesaleTotalAmountMessageDto wholesaleTotalAmountMessageDto, ISerializer serializer, Instant timestamp)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(wholesaleTotalAmountMessageDto);

        return new(
            wholesaleTotalAmountMessageDto.EventId,
            wholesaleTotalAmountMessageDto.DocumentType,
            Receiver.Create(wholesaleTotalAmountMessageDto.ReceiverNumber, wholesaleTotalAmountMessageDto.ReceiverRole),
            Receiver.Create(wholesaleTotalAmountMessageDto.ReceiverNumber, wholesaleTotalAmountMessageDto.ReceiverRole),
            wholesaleTotalAmountMessageDto.ProcessId,
            wholesaleTotalAmountMessageDto.BusinessReason,
            senderId: wholesaleTotalAmountMessageDto.SenderId,
            senderRole: wholesaleTotalAmountMessageDto.SenderRole,
            serializer.Serialize(wholesaleTotalAmountMessageDto.Series),
            timestamp,
            ProcessType.ReceiveWholesaleResults,
            wholesaleTotalAmountMessageDto.RelatedToMessageId,
            wholesaleTotalAmountMessageDto.Series.GridAreaCode,
            wholesaleTotalAmountMessageDto.ExternalId,
            wholesaleTotalAmountMessageDto.CalculationId);
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
            calculationId: null,
            documentReceiver: Receiver.Create(message.DocumentReceiverNumber, message.DocumentReceiverRole));
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
            calculationId: null,
            documentReceiver: Receiver.Create(message.DocumentReceiverNumber, message.DocumentReceiverRole));
    }

    private static ActorRole GetChargeOwnerRole(ActorNumber chargeOwnerId)
    {
        return chargeOwnerId == DataHubDetails.SystemOperatorActorNumber
            ? ActorRole.SystemOperator
            : ActorRole.GridOperator;
    }
}
