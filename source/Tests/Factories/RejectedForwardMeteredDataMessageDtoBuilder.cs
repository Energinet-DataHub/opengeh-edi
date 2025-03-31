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
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MeteredDataForMeteringPoint;

namespace Energinet.DataHub.EDI.Tests.Factories;

public class RejectedForwardMeteredDataMessageDtoBuilder
{
    private EventId _eventId = EventId.From(Guid.NewGuid());
    private ExternalId _externalId = new(Guid.NewGuid());
    private BusinessReason _businessReason = BusinessReason.PeriodicMetering;
    private Actor _receiver = new(ActorNumber.Create("1234567890123"), ActorRole.GridAccessProvider);
    private MessageId _relatedToMessageId = MessageId.New();
    private RejectedForwardMeteredDataSeries _series = new(
        RejectReasons: new List<RejectReason> { new("E01", "Test error message") },
        TransactionId: TransactionId.New(),
        OriginalTransactionIdReference: TransactionId.New());

    public RejectedForwardMeteredDataMessageDto Build()
    {
        return new RejectedForwardMeteredDataMessageDto(
            _eventId,
            _externalId,
            _businessReason,
            _receiver.ActorNumber,
            _receiver.ActorRole,
            _relatedToMessageId,
            _series);
    }

    public RejectedForwardMeteredDataMessageDtoBuilder WithEventId(EventId eventId)
    {
        _eventId = eventId;
        return this;
    }

    public RejectedForwardMeteredDataMessageDtoBuilder WithExternalId(ExternalId externalId)
    {
        _externalId = externalId;
        return this;
    }

    public RejectedForwardMeteredDataMessageDtoBuilder WithBusinessReason(BusinessReason businessReason)
    {
        _businessReason = businessReason;
        return this;
    }

    public RejectedForwardMeteredDataMessageDtoBuilder WithReceiver(Actor receiver)
    {
        _receiver = receiver;
        return this;
    }

    public RejectedForwardMeteredDataMessageDtoBuilder WithRelatedToMessageId(MessageId relatedToMessageId)
    {
        _relatedToMessageId = relatedToMessageId;
        return this;
    }

    public RejectedForwardMeteredDataMessageDtoBuilder WithSeries(RejectedForwardMeteredDataSeries series)
    {
        _series = series;
        return this;
    }
}
