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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MeteredDataForMeteringPoint;
using NodaTime;
using NodaTime.Text;

namespace Energinet.DataHub.EDI.Tests.Factories;

public class RejectedForwardMeteredDataMessageBuilder
{
#pragma warning disable SA1401
    public readonly BusinessReason BusinessReason;
    public readonly ActorNumber SenderId;
    public readonly ActorRole SenderRole;
    public readonly ActorNumber ReceiverId;
    public readonly ActorRole ReceiverRole;
    public readonly MessageId MessageId;
    public readonly MessageId RelatedToMessageId;
    public readonly TransactionId OriginalTransactionIdReference;
    public readonly Instant Timestamp;
#pragma warning restore SA1401
    private readonly List<RejectReason> _rejectReasons;
    private readonly EventId _eventId = EventId.From(Guid.NewGuid());
    private readonly ExternalId _externalId = new ExternalId(Guid.NewGuid());

    public RejectedForwardMeteredDataMessageBuilder(
        MessageId messageId,
        ActorNumber receiverId,
        ActorRole receiverRole,
        ActorNumber senderId,
        ActorRole senderRole,
        BusinessReason businessReason,
        MessageId relatedToMessageId,
        TransactionId originalTransactionIdReference,
        Instant timestamp)
    {
        _rejectReasons = new List<RejectReason>();
        ReceiverRole = receiverRole;
        SenderId = senderId;
        SenderRole = senderRole;
        BusinessReason = businessReason;
        RelatedToMessageId = relatedToMessageId;
        MessageId = messageId;
        ReceiverId = receiverId;
        OriginalTransactionIdReference = originalTransactionIdReference;
        Timestamp = timestamp;
    }

    public RejectedForwardMeteredDataMessageDto BuildDto()
    {
        return new RejectedForwardMeteredDataMessageDto(
            eventId: _eventId,
            externalId: _externalId,
            businessReason: BusinessReason,
            receiverId: ReceiverId,
            receiverRole: ReceiverRole,
            relatedToMessageId: RelatedToMessageId,
            series: GetSeries());
    }

    public OutgoingMessageHeader BuildHeader()
    {
        return new OutgoingMessageHeader(
            BusinessReason.Name,
            SenderId.Value,
            SenderRole.Code,
            ReceiverId.Value,
            ReceiverRole.Code,
            MessageId: MessageId.Value,
            RelatedToMessageId: RelatedToMessageId.Value,
            Timestamp);
    }

    public RejectedForwardMeteredDataSeries GetSeries()
    {
        var series = new RejectedForwardMeteredDataSeries(
            OriginalTransactionIdReference: OriginalTransactionIdReference,
#pragma warning disable SA1118
            RejectReasons: _rejectReasons.Count != 0
                ? _rejectReasons
                : new List<RejectReason>
                {
                    new RejectReason(
                        ErrorCode: "999",
                        ErrorMessage: "An error has occurred"),
                });

        return series;
    }

    public void AddReasonToSeries(RejectReason rejectReason)
    {
        _rejectReasons.Add(rejectReason);
    }
}
