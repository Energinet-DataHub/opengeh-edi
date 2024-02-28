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

    public IReadOnlyCollection<OutgoingMessage> CreateMessages(WholesaleMessageDto wholesaleMessageDto)
    {
        ArgumentNullException.ThrowIfNull(wholesaleMessageDto);
        return new List<OutgoingMessage>()
        {
            new(
                wholesaleMessageDto.DocumentType,
                wholesaleMessageDto.ReceiverId,
                wholesaleMessageDto.ProcessId,
                wholesaleMessageDto.BusinessReason,
                wholesaleMessageDto.ReceiverRole,
                wholesaleMessageDto.SenderId,
                wholesaleMessageDto.SenderRole,
                wholesaleMessageDto.SerializedContent,
                _systemDateTimeProvider.Now(),
                wholesaleMessageDto.RelatedToMessageId),
            new(
                wholesaleMessageDto.DocumentType,
                wholesaleMessageDto.ChargeOwner,
                wholesaleMessageDto.ProcessId,
                wholesaleMessageDto.BusinessReason,
                GetChargeOwnerRole(wholesaleMessageDto.ChargeOwner),
                wholesaleMessageDto.SenderId,
                wholesaleMessageDto.SenderRole,
                wholesaleMessageDto.SerializedContent,
                _systemDateTimeProvider.Now(),
                wholesaleMessageDto.RelatedToMessageId),
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
