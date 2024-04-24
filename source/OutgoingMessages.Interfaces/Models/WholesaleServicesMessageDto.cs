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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;

public class WholesaleServicesMessageDto : OutgoingMessageDto
{
    protected WholesaleServicesMessageDto(
        ActorNumber receiverNumber,
        Guid? processId,
        EventId eventId,
        string businessReason,
        ActorRole receiverRole,
        ActorNumber chargeOwnerId,
        WholesaleServicesSeries series,
        MessageId? relatedToMessageId = null)
        : base(
            DocumentType.NotifyWholesaleServices,
            receiverNumber,
            processId,
            eventId,
            businessReason,
            receiverRole,
            DataHubDetails.DataHubActorNumber,
            ActorRole.MeteredDataAdministrator,
            relatedToMessageId)
    {
        ChargeOwnerId = chargeOwnerId;
        Series = series;
    }

    public ActorNumber ChargeOwnerId { get; }

    public WholesaleServicesSeries Series { get; }

    public static WholesaleServicesMessageDto Create(
        EventId eventId,
        ActorNumber receiverNumber,
        ActorRole receiverRole,
        ActorNumber chargeOwnerId,
        string businessReason,
        WholesaleServicesSeries wholesaleSeries)
    {
        ArgumentNullException.ThrowIfNull(eventId);
        ArgumentNullException.ThrowIfNull(businessReason);

        return new WholesaleServicesMessageDto(
            receiverNumber: receiverNumber,
            receiverRole: receiverRole,
            processId: null,
            eventId: eventId,
            businessReason: businessReason,
            series: wholesaleSeries,
            chargeOwnerId: chargeOwnerId);
    }
}
