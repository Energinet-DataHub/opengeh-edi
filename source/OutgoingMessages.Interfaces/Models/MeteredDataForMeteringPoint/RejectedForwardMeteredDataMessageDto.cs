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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

namespace Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MeteredDataForMeteringPoint;

public sealed class RejectedForwardMeteredDataMessageDto : OutgoingMessageDto
{
    public RejectedForwardMeteredDataMessageDto(
        EventId eventId,
        ExternalId externalId,
        BusinessReason businessReason,
        ActorNumber receiverId,
        ActorRole receiverRole,
        MessageId relatedToMessageId,
        RejectedForwardMeteredDataSeries series)
        : base(
            DocumentType.Acknowledgement,
            receiverId,
            Guid.NewGuid(),
            eventId,
            businessReason.Name,
            receiverRole,
            externalId,
            relatedToMessageId)
    {
        Series = series;
    }

    public RejectedForwardMeteredDataSeries Series { get; }
}

public record RejectedForwardMeteredDataSeries(
    TransactionId TransactionId,
    IReadOnlyCollection<RejectedRejectedForwardMeteredDataReason> RejectReasons,
    TransactionId OriginalTransactionIdReference);

public record RejectedRejectedForwardMeteredDataReason(string ErrorCode, string ErrorMessage);
