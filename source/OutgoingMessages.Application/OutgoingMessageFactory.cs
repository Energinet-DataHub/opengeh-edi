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

        var receiver = Receiver.Create(acceptedMessage.ReceiverNumber, acceptedMessage.ReceiverRole);
        var serializedContent = serializer.Serialize(acceptedMessage.Series);
        return new OutgoingMessage(
            eventId: acceptedMessage.EventId,
            documentType: acceptedMessage.DocumentType,
            receiver: receiver,
            processId: acceptedMessage.ProcessId,
            businessReason: acceptedMessage.BusinessReason,
            serializedContent: serializedContent,
            createdAt: timestamp,
            messageCreatedFromProcess: ProcessType.RequestEnergyResults,
            relatedToMessageId: acceptedMessage.RelatedToMessageId,
            gridAreaCode: acceptedMessage.Series.GridAreaCode,
            externalId: acceptedMessage.ExternalId,
            calculationId: null,
            documentReceiver: Receiver.Create(
                acceptedMessage.DocumentReceiverNumber,
                acceptedMessage.DocumentReceiverRole),
            idempotentId: IdempotentId.From(
                $"{receiver}-{acceptedMessage.ExternalId}-{serializedContent.GetHashCode()}"));
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

        var receiver = Receiver.Create(rejectedMessage.ReceiverNumber, rejectedMessage.ReceiverRole);
        var serializedContent = serializer.Serialize(rejectedMessage.Series);
        return new OutgoingMessage(
            eventId: rejectedMessage.EventId,
            documentType: rejectedMessage.DocumentType,
            receiver: receiver,
            processId: rejectedMessage.ProcessId,
            businessReason: rejectedMessage.BusinessReason,
            serializedContent: serializedContent,
            createdAt: timestamp,
            messageCreatedFromProcess: ProcessType.RequestEnergyResults,
            relatedToMessageId: rejectedMessage.RelatedToMessageId,
            gridAreaCode: null,
            externalId: rejectedMessage.ExternalId,
            calculationId: null,
            documentReceiver: Receiver.Create(
                rejectedMessage.DocumentReceiverNumber,
                rejectedMessage.DocumentReceiverRole),
            idempotentId: IdempotentId.From(
                $"{receiver}-{rejectedMessage.ExternalId}-{serializedContent.GetHashCode()}"));
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

        var receiver = Receiver.Create(messageDto.ReceiverNumber, messageDto.ReceiverRole);
        var serializedContent = serializer.Serialize(messageDto.Series);
        return new OutgoingMessage(
            messageDto.EventId,
            messageDto.DocumentType,
            receiver,
            receiver,
            messageDto.ProcessId,
            messageDto.BusinessReason,
            serializedContent,
            timestamp,
            ProcessType.ReceiveEnergyResults,
            messageDto.RelatedToMessageId,
            messageDto.Series.GridAreaCode,
            messageDto.ExternalId,
            messageDto.CalculationId,
            idempotentId: IdempotentId.From($"{receiver}-{messageDto.ExternalId}-{serializedContent.GetHashCode()}"));
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

        var receiver = Receiver.Create(messageDto.ReceiverNumber, messageDto.ReceiverRole);
        var serializedContent = serializer.Serialize(messageDto.Series);
        return new OutgoingMessage(
            messageDto.EventId,
            messageDto.DocumentType,
            receiver,
            receiver,
            messageDto.ProcessId,
            messageDto.BusinessReason,
            serializedContent,
            timestamp,
            ProcessType.ReceiveEnergyResults,
            messageDto.RelatedToMessageId,
            messageDto.Series.GridAreaCode,
            messageDto.ExternalId,
            messageDto.CalculationId,
            idempotentId: IdempotentId.From($"{receiver}-{messageDto.ExternalId}-{serializedContent.GetHashCode()}"));
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

        var receiver = Receiver.Create(messageDto.EnergySupplierNumber, ActorRole.EnergySupplier);
        var serializedContent = serializer.Serialize(messageDto.SeriesForEnergySupplier);
        List<OutgoingMessage> outgoingMessages = [
            new OutgoingMessage(
                eventId: messageDto.EventId,
                documentType: messageDto.DocumentType,
                processId: messageDto.ProcessId,
                businessReason: messageDto.BusinessReason,
                receiver: receiver,
                // TODO: Is the document receiver correct?
                documentReceiver: Receiver.Create(messageDto.BalanceResponsibleNumber, ActorRole.EnergySupplier),
                serializedContent: serializedContent,
                createdAt: timestamp,
                messageCreatedFromProcess: ProcessType.ReceiveEnergyResults,
                relatedToMessageId: messageDto.RelatedToMessageId,
                gridAreaCode: messageDto.GridArea,
                externalId: messageDto.ExternalId,
                calculationId: messageDto.CalculationId,
                idempotentId: IdempotentId.From(
                    $"{receiver}-{messageDto.ExternalId}-{serializedContent.GetHashCode()}")),
        ];

        // Only create a message for the balance responsible if the business reason is BalanceFixing or PreliminaryAggregation
        if (messageDto.BusinessReason is not DataHubNames.BusinessReason.WholesaleFixing &&
            messageDto.BusinessReason is not DataHubNames.BusinessReason.Correction)
        {
            var balanceResponsibleReceiver = Receiver.Create(
                messageDto.BalanceResponsibleNumber,
                ActorRole.BalanceResponsibleParty);
            var outgoingMessageToBalanceResponsible = new OutgoingMessage(
                eventId: messageDto.EventId,
                documentType: messageDto.DocumentType,
                processId: messageDto.ProcessId,
                businessReason: messageDto.BusinessReason,
                receiver: balanceResponsibleReceiver,
                documentReceiver: balanceResponsibleReceiver,
                serializedContent: serializer.Serialize(messageDto.SeriesForBalanceResponsible),
                createdAt: timestamp,
                messageCreatedFromProcess: ProcessType.ReceiveEnergyResults,
                relatedToMessageId: messageDto.RelatedToMessageId,
                gridAreaCode: messageDto.GridArea,
                externalId: messageDto.ExternalId,
                calculationId: messageDto.CalculationId,
                idempotentId: IdempotentId.From(
                    $"{balanceResponsibleReceiver}-{messageDto.ExternalId}-{serializedContent.GetHashCode()}"));

            outgoingMessages.Add(outgoingMessageToBalanceResponsible);
        }

        return outgoingMessages;
    }

    /// <summary>
    /// This method creates two outgoing messages, one for the receiver and one for the charge owner,
    /// based on the wholesaleResultMessage.
    /// </summary>
    public static IReadOnlyCollection<OutgoingMessage> CreateMessages(
        WholesaleAmountPerChargeMessageDto wholesaleAmountPerChargeMessageDto,
        ISerializer serializer,
        Instant timestamp)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(wholesaleAmountPerChargeMessageDto);

        var energySupplierReceiver = Receiver.Create(
            wholesaleAmountPerChargeMessageDto.EnergySupplierReceiverId,
            ActorRole.EnergySupplier);
        var serializedContent = serializer.Serialize(wholesaleAmountPerChargeMessageDto.Series);
        var chargeOwnerReceiver = Receiver.Create(
            wholesaleAmountPerChargeMessageDto.ChargeOwnerReceiverId,
            GetChargeOwnerRole(wholesaleAmountPerChargeMessageDto.ChargeOwnerReceiverId));
        return new List<OutgoingMessage>()
        {
            new(
                wholesaleAmountPerChargeMessageDto.EventId,
                wholesaleAmountPerChargeMessageDto.DocumentType,
                energySupplierReceiver,
                energySupplierReceiver,
                wholesaleAmountPerChargeMessageDto.ProcessId,
                wholesaleAmountPerChargeMessageDto.BusinessReason,
                serializedContent,
                timestamp,
                ProcessType.ReceiveWholesaleResults,
                wholesaleAmountPerChargeMessageDto.RelatedToMessageId,
                wholesaleAmountPerChargeMessageDto.Series.GridAreaCode,
                wholesaleAmountPerChargeMessageDto.ExternalId,
                wholesaleAmountPerChargeMessageDto.CalculationId,
                idempotentId: IdempotentId.From(
                    $"{energySupplierReceiver}-{wholesaleAmountPerChargeMessageDto.ExternalId}-{serializedContent.GetHashCode()}")),
            new(
                wholesaleAmountPerChargeMessageDto.EventId,
                wholesaleAmountPerChargeMessageDto.DocumentType,
                chargeOwnerReceiver,
                chargeOwnerReceiver,
                wholesaleAmountPerChargeMessageDto.ProcessId,
                wholesaleAmountPerChargeMessageDto.BusinessReason,
                serializedContent,
                timestamp,
                ProcessType.ReceiveWholesaleResults,
                wholesaleAmountPerChargeMessageDto.RelatedToMessageId,
                wholesaleAmountPerChargeMessageDto.Series.GridAreaCode,
                wholesaleAmountPerChargeMessageDto.ExternalId,
                wholesaleAmountPerChargeMessageDto.CalculationId,
                idempotentId: IdempotentId.From(
                    $"{chargeOwnerReceiver}-{wholesaleAmountPerChargeMessageDto.ExternalId}-{serializedContent.GetHashCode()}")),
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

        var energySupplierReceiver = Receiver.Create(
            wholesaleMonthlyAmountPerChargeMessageDto.EnergySupplierReceiverId,
            ActorRole.EnergySupplier);
        var serializedContent = serializer.Serialize(wholesaleMonthlyAmountPerChargeMessageDto.Series);
        var chargeOwnerReceiver = Receiver.Create(
            wholesaleMonthlyAmountPerChargeMessageDto.ChargeOwnerReceiverId,
            GetChargeOwnerRole(wholesaleMonthlyAmountPerChargeMessageDto.ChargeOwnerReceiverId));
        return new List<OutgoingMessage>
        {
            new(
                wholesaleMonthlyAmountPerChargeMessageDto.EventId,
                wholesaleMonthlyAmountPerChargeMessageDto.DocumentType,
                energySupplierReceiver,
                energySupplierReceiver,
                wholesaleMonthlyAmountPerChargeMessageDto.ProcessId,
                wholesaleMonthlyAmountPerChargeMessageDto.BusinessReason,
                serializedContent,
                timestamp,
                ProcessType.ReceiveWholesaleResults,
                wholesaleMonthlyAmountPerChargeMessageDto.RelatedToMessageId,
                wholesaleMonthlyAmountPerChargeMessageDto.Series.GridAreaCode,
                wholesaleMonthlyAmountPerChargeMessageDto.ExternalId,
                wholesaleMonthlyAmountPerChargeMessageDto.CalculationId,
                idempotentId: IdempotentId.From(
                    $"{energySupplierReceiver}-{wholesaleMonthlyAmountPerChargeMessageDto.ExternalId}-{serializedContent.GetHashCode()}")),
            new(
                wholesaleMonthlyAmountPerChargeMessageDto.EventId,
                wholesaleMonthlyAmountPerChargeMessageDto.DocumentType,
                chargeOwnerReceiver,
                chargeOwnerReceiver,
                wholesaleMonthlyAmountPerChargeMessageDto.ProcessId,
                wholesaleMonthlyAmountPerChargeMessageDto.BusinessReason,
                serializedContent,
                timestamp,
                ProcessType.ReceiveWholesaleResults,
                wholesaleMonthlyAmountPerChargeMessageDto.RelatedToMessageId,
                wholesaleMonthlyAmountPerChargeMessageDto.Series.GridAreaCode,
                wholesaleMonthlyAmountPerChargeMessageDto.ExternalId,
                wholesaleMonthlyAmountPerChargeMessageDto.CalculationId,
                idempotentId: IdempotentId.From(
                    $"{chargeOwnerReceiver}-{wholesaleMonthlyAmountPerChargeMessageDto.ExternalId}-{serializedContent.GetHashCode()}")),
        };
    }

    /// <summary>
    /// This method creates an outgoing message, one for the receiver based on the WholesaleTotalAmountMessageDto.
    /// </summary>
    public static OutgoingMessage CreateMessage(
        WholesaleTotalAmountMessageDto wholesaleTotalAmountMessageDto,
        ISerializer serializer,
        Instant timestamp)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(wholesaleTotalAmountMessageDto);

        var receiver = Receiver.Create(
            wholesaleTotalAmountMessageDto.ReceiverNumber,
            wholesaleTotalAmountMessageDto.ReceiverRole);
        var serializedContent = serializer.Serialize(wholesaleTotalAmountMessageDto.Series);
        return new(
            wholesaleTotalAmountMessageDto.EventId,
            wholesaleTotalAmountMessageDto.DocumentType,
            receiver,
            receiver,
            wholesaleTotalAmountMessageDto.ProcessId,
            wholesaleTotalAmountMessageDto.BusinessReason,
            serializedContent,
            timestamp,
            ProcessType.ReceiveWholesaleResults,
            wholesaleTotalAmountMessageDto.RelatedToMessageId,
            wholesaleTotalAmountMessageDto.Series.GridAreaCode,
            wholesaleTotalAmountMessageDto.ExternalId,
            wholesaleTotalAmountMessageDto.CalculationId,
            idempotentId: IdempotentId.From(
                $"{receiver}-{wholesaleTotalAmountMessageDto.ExternalId}-{serializedContent.GetHashCode()}"));
    }

    /// <summary>
    /// This method create a single outgoing message, for the receiver, based on the rejected WholesaleServicesMessage.
    /// </summary>
    public static OutgoingMessage CreateMessage(
        RejectedWholesaleServicesMessageDto message,
        ISerializer serializer,
        Instant timestamp)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(message);

        var receiver = Receiver.Create(message.ReceiverNumber, message.ReceiverRole);
        var serializedContent = serializer.Serialize(message.Series);
        return new OutgoingMessage(
            eventId: message.EventId,
            documentType: message.DocumentType,
            receiver: receiver,
            processId: message.ProcessId,
            businessReason: message.BusinessReason,
            serializedContent: serializedContent,
            createdAt: timestamp,
            messageCreatedFromProcess: ProcessType.RequestWholesaleResults,
            relatedToMessageId: message.RelatedToMessageId,
            gridAreaCode: null,
            externalId: message.ExternalId,
            calculationId: null,
            documentReceiver: Receiver.Create(message.DocumentReceiverNumber, message.DocumentReceiverRole),
            idempotentId: IdempotentId.From(
                $"{receiver}-{message.ExternalId}-{serializedContent.GetHashCode()}"));
    }

    /// <summary>
    /// This method create a single outgoing message, for the receiver,
    /// based on the accepted WholesaleServicesMessage.
    /// </summary>
    public static OutgoingMessage CreateMessage(
        AcceptedWholesaleServicesMessageDto message,
        ISerializer serializer,
        Instant timestamp)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(message);

        var receiver = Receiver.Create(message.ReceiverNumber, message.ReceiverRole);
        var serializedContent = serializer.Serialize(message.Series);
        return new OutgoingMessage(
            eventId: message.EventId,
            documentType: message.DocumentType,
            receiver: receiver,
            processId: message.ProcessId,
            businessReason: message.BusinessReason,
            serializedContent: serializedContent,
            createdAt: timestamp,
            messageCreatedFromProcess: ProcessType.RequestWholesaleResults,
            relatedToMessageId: message.RelatedToMessageId,
            gridAreaCode: message.Series.GridAreaCode,
            externalId: message.ExternalId,
            calculationId: null,
            documentReceiver: Receiver.Create(message.DocumentReceiverNumber, message.DocumentReceiverRole),
            idempotentId: IdempotentId.From(
                $"{receiver}-{message.ExternalId}-{serializedContent.GetHashCode()}"));
    }

    private static ActorRole GetChargeOwnerRole(ActorNumber chargeOwnerId)
    {
        return chargeOwnerId == DataHubDetails.SystemOperatorActorNumber
            ? ActorRole.SystemOperator
            : ActorRole.GridOperator;
    }
}
