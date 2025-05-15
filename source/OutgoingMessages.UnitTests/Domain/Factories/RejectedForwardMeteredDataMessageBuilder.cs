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

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.Factories;

public class RejectedForwardMeteredDataMessageBuilder
{
    private readonly List<RejectReason> _rejectReasons;
    private readonly EventId _eventId = EventId.From(Guid.NewGuid());
    private readonly ExternalId _externalId = ExternalId.New();

    public RejectedForwardMeteredDataMessageBuilder(
        MessageId messageId,
        ActorNumber receiverId,
        ActorRole receiverRole,
        ActorNumber senderId,
        ActorRole senderRole,
        BusinessReason businessReason,
        MessageId relatedToMessageId,
        TransactionId originalTransactionIdReference,
        TransactionId transactionId,
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
        TransactionId = transactionId;
        Timestamp = timestamp;
    }

    public BusinessReason BusinessReason { get; }

    public ActorNumber SenderId { get; }

    public ActorRole SenderRole { get; }

    public ActorNumber ReceiverId { get; }

    public ActorRole ReceiverRole { get; }

    public MessageId MessageId { get; }

    public MessageId RelatedToMessageId { get; }

    public TransactionId OriginalTransactionIdReference { get; }

    public TransactionId TransactionId { get; }

    public Instant Timestamp { get; }

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
            TransactionId: TransactionId,
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
