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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Common.DateTime;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application;

public class OutgoingMessageFactory
{
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;

    public OutgoingMessageFactory(ISystemDateTimeProvider systemDateTimeProvider)
    {
        _systemDateTimeProvider = systemDateTimeProvider;
    }

    /// <summary>
    /// This method create a single outgoing message, for the receiver, based on the energyResultMessage.
    /// </summary>
    /// <param name="energyResultMessage"></param>
    public OutgoingMessage CreateMessage(EnergyResultMessageDto energyResultMessage)
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
            energyResultMessage.SerializedContent,
            _systemDateTimeProvider.Now(),
            energyResultMessage.RelatedToMessageId);
    }

    /// <summary>
    /// This method create a single outgoing message, for the receiver, based on the accepted energyResultMessage.
    /// </summary>
    /// <param name="acceptedEnergyResultMessage"></param>
    public OutgoingMessage CreateMessage(AcceptedEnergyResultMessageDto acceptedEnergyResultMessage)
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
            acceptedEnergyResultMessage.SerializedContent,
            _systemDateTimeProvider.Now(),
            acceptedEnergyResultMessage.RelatedToMessageId);
    }

    /// <summary>
    /// This method create a single outgoing message, for the receiver, based on the rejected energyResultMessage.
    /// </summary>
    /// <param name="rejectedEnergyResultMessage"></param>
    public OutgoingMessage CreateMessage(RejectedEnergyResultMessageDto rejectedEnergyResultMessage)
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
            rejectedEnergyResultMessage.SerializedContent,
            _systemDateTimeProvider.Now(),
            rejectedEnergyResultMessage.RelatedToMessageId);
    }

    /// <summary>
    /// This method creates two outgoing messages, one for the receiver and one for the charge owner, based on the wholesaleResultMessage.
    /// </summary>
    /// <param name="wholesaleResultMessageDto"></param>
    public IReadOnlyCollection<OutgoingMessage> CreateMessages(WholesaleResultMessageDto wholesaleResultMessageDto)
    {
        ArgumentNullException.ThrowIfNull(wholesaleResultMessageDto);
        return new List<OutgoingMessage>()
        {
            new(
                wholesaleResultMessageDto.DocumentType,
                wholesaleResultMessageDto.ReceiverId,
                wholesaleResultMessageDto.ProcessId,
                wholesaleResultMessageDto.BusinessReason,
                wholesaleResultMessageDto.ReceiverRole,
                wholesaleResultMessageDto.SenderId,
                wholesaleResultMessageDto.SenderRole,
                wholesaleResultMessageDto.SerializedContent,
                _systemDateTimeProvider.Now(),
                wholesaleResultMessageDto.RelatedToMessageId),
            new(
                wholesaleResultMessageDto.DocumentType,
                wholesaleResultMessageDto.ChargeOwnerId,
                wholesaleResultMessageDto.ProcessId,
                wholesaleResultMessageDto.BusinessReason,
                GetChargeOwnerRole(wholesaleResultMessageDto.ChargeOwnerId),
                wholesaleResultMessageDto.SenderId,
                wholesaleResultMessageDto.SenderRole,
                wholesaleResultMessageDto.SerializedContent,
                _systemDateTimeProvider.Now(),
                wholesaleResultMessageDto.RelatedToMessageId),
        };
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
