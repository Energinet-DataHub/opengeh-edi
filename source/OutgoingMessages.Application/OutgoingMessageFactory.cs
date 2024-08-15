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
            documentReceiver: Receiver.Create(acceptedMessage.DocumentReceiverNumber, acceptedMessage.DocumentReceiverRole),
            processId: acceptedMessage.ProcessId,
            businessReason: acceptedMessage.BusinessReason,
            serializedContent: serializer.Serialize(acceptedMessage.Series),
            createdAt: timestamp,
            messageCreatedFromProcess: ProcessType.RequestEnergyResults,
            relatedToMessageId: acceptedMessage.RelatedToMessageId,
            gridAreaCode: acceptedMessage.Series.GridAreaCode,
            externalId: acceptedMessage.ExternalId,
            calculationId: null,
            OutgoingMessageIdempotentId.New(
                acceptedMessage.ReceiverRole.Code,
                acceptedMessage.EventId.Value,
                acceptedMessage.Series.Period.ToString()));
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
            serializedContent: serializer.Serialize(rejectedMessage.Series),
            createdAt: timestamp,
            messageCreatedFromProcess: ProcessType.RequestEnergyResults,
            relatedToMessageId: rejectedMessage.RelatedToMessageId,
            gridAreaCode: null,
            externalId: rejectedMessage.ExternalId,
            calculationId: null,
            OutgoingMessageIdempotentId.New(rejectedMessage.ReceiverRole.Code, rejectedMessage.EventId.Value));
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
            serializer.Serialize(messageDto.Series),
            timestamp,
            ProcessType.ReceiveEnergyResults,
            messageDto.RelatedToMessageId,
            messageDto.Series.GridAreaCode,
            messageDto.ExternalId,
            messageDto.CalculationId,
            OutgoingMessageIdempotentId.New(
                messageDto.ReceiverRole.Code,
                messageDto.EventId.Value,
                messageDto.Series.Period.ToString()));
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
            serializer.Serialize(messageDto.Series),
            timestamp,
            ProcessType.ReceiveEnergyResults,
            messageDto.RelatedToMessageId,
            messageDto.Series.GridAreaCode,
            messageDto.ExternalId,
            messageDto.CalculationId,
            OutgoingMessageIdempotentId.New(
                messageDto.ReceiverRole.Code,
                messageDto.EventId.Value,
                messageDto.Series.Period.ToString()));
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
                receiver: Receiver.Create(messageDto.EnergySupplierNumber, ActorRole.EnergySupplier),
                documentReceiver: Receiver.Create(messageDto.BalanceResponsibleNumber, ActorRole.EnergySupplier), // TODO: Should energy supplier be the document receiver?
                processId: messageDto.ProcessId,
                businessReason: messageDto.BusinessReason,
                serializedContent: serializer.Serialize(messageDto.SeriesForEnergySupplier),
                createdAt: timestamp,
                messageCreatedFromProcess: ProcessType.ReceiveEnergyResults,
                relatedToMessageId: messageDto.RelatedToMessageId,
                gridAreaCode: messageDto.GridArea,
                externalId: messageDto.ExternalId,
                calculationId: messageDto.CalculationId,
                OutgoingMessageIdempotentId.New(
                    ActorRole.EnergySupplier.Code,
                    messageDto.EventId.Value,
                    messageDto.SeriesForEnergySupplier.Period.ToString())),
        ];

        // Only create a message for the balance responsible if the business reason is BalanceFixing or PreliminaryAggregation
        if (messageDto.BusinessReason is not DataHubNames.BusinessReason.WholesaleFixing &&
            messageDto.BusinessReason is not DataHubNames.BusinessReason.Correction)
        {
            var outgoingMessageToBalanceResponsible = new OutgoingMessage(
                eventId: messageDto.EventId,
                documentType: messageDto.DocumentType,
                receiver: Receiver.Create(messageDto.BalanceResponsibleNumber, ActorRole.BalanceResponsibleParty),
                documentReceiver: Receiver.Create(messageDto.BalanceResponsibleNumber, ActorRole.BalanceResponsibleParty),
                processId: messageDto.ProcessId,
                businessReason: messageDto.BusinessReason,
                serializedContent: serializer.Serialize(messageDto.SeriesForBalanceResponsible),
                createdAt: timestamp,
                messageCreatedFromProcess: ProcessType.ReceiveEnergyResults,
                relatedToMessageId: messageDto.RelatedToMessageId,
                gridAreaCode: messageDto.GridArea,
                externalId: messageDto.ExternalId,
                calculationId: messageDto.CalculationId,
                OutgoingMessageIdempotentId.New(
                    ActorRole.BalanceResponsibleParty.Code,
                    messageDto.EventId.Value,
                    messageDto.SeriesForBalanceResponsible.Period.ToString()));

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

        var chargeOwnerRole = GetChargeOwnerRole(wholesaleAmountPerChargeMessageDto.ChargeOwnerReceiverId);
        return new List<OutgoingMessage>()
        {
            new(
                wholesaleAmountPerChargeMessageDto.EventId,
                wholesaleAmountPerChargeMessageDto.DocumentType,
                Receiver.Create(wholesaleAmountPerChargeMessageDto.EnergySupplierReceiverId, ActorRole.EnergySupplier),
                Receiver.Create(wholesaleAmountPerChargeMessageDto.EnergySupplierReceiverId, ActorRole.EnergySupplier),
                wholesaleAmountPerChargeMessageDto.ProcessId,
                wholesaleAmountPerChargeMessageDto.BusinessReason,
                serializer.Serialize(wholesaleAmountPerChargeMessageDto.Series),
                timestamp,
                ProcessType.ReceiveWholesaleResults,
                wholesaleAmountPerChargeMessageDto.RelatedToMessageId,
                wholesaleAmountPerChargeMessageDto.Series.GridAreaCode,
                wholesaleAmountPerChargeMessageDto.ExternalId,
                wholesaleAmountPerChargeMessageDto.CalculationId,
                OutgoingMessageIdempotentId.New(
                    ActorRole.EnergySupplier.Code,
                    wholesaleAmountPerChargeMessageDto.EventId.Value,
                    wholesaleAmountPerChargeMessageDto.Series.Period.ToString())),
            new(
                wholesaleAmountPerChargeMessageDto.EventId,
                wholesaleAmountPerChargeMessageDto.DocumentType,
                Receiver.Create(
                    wholesaleAmountPerChargeMessageDto.ChargeOwnerReceiverId,
                    chargeOwnerRole),
                Receiver.Create(
                    wholesaleAmountPerChargeMessageDto.ChargeOwnerReceiverId,
                    chargeOwnerRole),
                wholesaleAmountPerChargeMessageDto.ProcessId,
                wholesaleAmountPerChargeMessageDto.BusinessReason,
                serializer.Serialize(wholesaleAmountPerChargeMessageDto.Series),
                timestamp,
                ProcessType.ReceiveWholesaleResults,
                wholesaleAmountPerChargeMessageDto.RelatedToMessageId,
                wholesaleAmountPerChargeMessageDto.Series.GridAreaCode,
                wholesaleAmountPerChargeMessageDto.ExternalId,
                wholesaleAmountPerChargeMessageDto.CalculationId,
                OutgoingMessageIdempotentId.New(
                    chargeOwnerRole.Code,
                    wholesaleAmountPerChargeMessageDto.EventId.Value,
                    wholesaleAmountPerChargeMessageDto.Series.Period.ToString())),
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

        var chargeOwnerRole = GetChargeOwnerRole(wholesaleMonthlyAmountPerChargeMessageDto.ChargeOwnerReceiverId);
        return new List<OutgoingMessage>
        {
            new(
                wholesaleMonthlyAmountPerChargeMessageDto.EventId,
                wholesaleMonthlyAmountPerChargeMessageDto.DocumentType,
                Receiver.Create(wholesaleMonthlyAmountPerChargeMessageDto.EnergySupplierReceiverId, ActorRole.EnergySupplier),
                Receiver.Create(wholesaleMonthlyAmountPerChargeMessageDto.EnergySupplierReceiverId, ActorRole.EnergySupplier),
                wholesaleMonthlyAmountPerChargeMessageDto.ProcessId,
                wholesaleMonthlyAmountPerChargeMessageDto.BusinessReason,
                serializer.Serialize(wholesaleMonthlyAmountPerChargeMessageDto.Series),
                timestamp,
                ProcessType.ReceiveWholesaleResults,
                wholesaleMonthlyAmountPerChargeMessageDto.RelatedToMessageId,
                wholesaleMonthlyAmountPerChargeMessageDto.Series.GridAreaCode,
                wholesaleMonthlyAmountPerChargeMessageDto.ExternalId,
                wholesaleMonthlyAmountPerChargeMessageDto.CalculationId,
                OutgoingMessageIdempotentId.New(
                    ActorRole.EnergySupplier.Code,
                    wholesaleMonthlyAmountPerChargeMessageDto.EventId.Value,
                    wholesaleMonthlyAmountPerChargeMessageDto.Series.Period.ToString())),
            new(
                wholesaleMonthlyAmountPerChargeMessageDto.EventId,
                wholesaleMonthlyAmountPerChargeMessageDto.DocumentType,
                Receiver.Create(
                    wholesaleMonthlyAmountPerChargeMessageDto.ChargeOwnerReceiverId,
                    chargeOwnerRole),
                Receiver.Create(
                    wholesaleMonthlyAmountPerChargeMessageDto.ChargeOwnerReceiverId,
                    chargeOwnerRole),
                wholesaleMonthlyAmountPerChargeMessageDto.ProcessId,
                wholesaleMonthlyAmountPerChargeMessageDto.BusinessReason,
                serializer.Serialize(wholesaleMonthlyAmountPerChargeMessageDto.Series),
                timestamp,
                ProcessType.ReceiveWholesaleResults,
                wholesaleMonthlyAmountPerChargeMessageDto.RelatedToMessageId,
                wholesaleMonthlyAmountPerChargeMessageDto.Series.GridAreaCode,
                wholesaleMonthlyAmountPerChargeMessageDto.ExternalId,
                wholesaleMonthlyAmountPerChargeMessageDto.CalculationId,
                OutgoingMessageIdempotentId.New(
                    chargeOwnerRole.Code,
                    wholesaleMonthlyAmountPerChargeMessageDto.EventId.Value,
                    wholesaleMonthlyAmountPerChargeMessageDto.Series.Period.ToString())),
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
            serializer.Serialize(wholesaleTotalAmountMessageDto.Series),
            timestamp,
            ProcessType.ReceiveWholesaleResults,
            wholesaleTotalAmountMessageDto.RelatedToMessageId,
            wholesaleTotalAmountMessageDto.Series.GridAreaCode,
            wholesaleTotalAmountMessageDto.ExternalId,
            wholesaleTotalAmountMessageDto.CalculationId,
            OutgoingMessageIdempotentId.New(
                wholesaleTotalAmountMessageDto.ReceiverRole.Code,
                wholesaleTotalAmountMessageDto.EventId.Value,
                wholesaleTotalAmountMessageDto.Series.Period.ToString()));
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
            serializedContent: serializer.Serialize(message.Series),
            createdAt: timestamp,
            messageCreatedFromProcess: ProcessType.RequestWholesaleResults,
            relatedToMessageId: message.RelatedToMessageId,
            gridAreaCode: null,
            externalId: message.ExternalId,
            calculationId: null,
            OutgoingMessageIdempotentId.New(message.ReceiverRole.Code, message.EventId.Value));
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
            serializedContent: serializer.Serialize(message.Series),
            createdAt: timestamp,
            messageCreatedFromProcess: ProcessType.RequestWholesaleResults,
            relatedToMessageId: message.RelatedToMessageId,
            gridAreaCode: message.Series.GridAreaCode,
            externalId: message.ExternalId,
            calculationId: null,
            OutgoingMessageIdempotentId.New(
                message.ReceiverRole.Code,
                message.EventId.Value,
                message.Series.Period.ToString()));
    }

    private static ActorRole GetChargeOwnerRole(ActorNumber chargeOwnerId)
    {
        return chargeOwnerId == DataHubDetails.SystemOperatorActorNumber
            ? ActorRole.SystemOperator
            : ActorRole.GridOperator;
    }
}
