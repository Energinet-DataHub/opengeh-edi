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
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MissingMeasurementMessages;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.Factories;

public class MissingMeasurementMessageBuilder
{
    private readonly List<MissingMeasurement> _missingMeasurements;
    private readonly EventId _eventId = EventId.From(Guid.NewGuid());
    private readonly ExternalId _externalId = new ExternalId(Guid.NewGuid());

    public MissingMeasurementMessageBuilder(
        MessageId messageId,
        ActorNumber receiverId,
        ActorRole receiverRole,
        ActorNumber senderId,
        ActorRole senderRole,
        BusinessReason businessReason,
        TransactionId transactionId,
        Instant timestamp)
    {
        ReceiverRole = receiverRole;
        SenderId = senderId;
        SenderRole = senderRole;
        BusinessReason = businessReason;
        MessageId = messageId;
        ReceiverId = receiverId;
        TransactionId = transactionId;
        Timestamp = timestamp;
        _missingMeasurements = [];
    }

    public BusinessReason BusinessReason { get; }

    public ActorNumber SenderId { get; }

    public ActorRole SenderRole { get; }

    public ActorNumber ReceiverId { get; }

    public ActorRole ReceiverRole { get; }

    public MessageId MessageId { get; }

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
            RelatedToMessageId: null,
            Timestamp);
    }

    public IReadOnlyCollection<MissingMeasurement> GetSeries() => _missingMeasurements;

    public void AddMissingMeasurement(MissingMeasurement missingMeasurement)
    {
        _missingMeasurements.Add(missingMeasurement);
    }
}
