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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.EnergyResultMessages.Request;
using NodaTime;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace Energinet.DataHub.EDI.IntegrationTests.Factories;

public class RejectedEnergyResultMessageDtoBuilder
{
    private static readonly EventId _eventId = EventId.From(Guid.NewGuid());
    private static readonly Guid _processId = Guid.NewGuid();
    private static readonly string _businessReason = BusinessReason.BalanceFixing.Name;
    private static readonly RejectedEnergyResultMessageSerie _series = new(
        TransactionId.From("4E85A732-85FD-4D92-8FF3-72C052802716"),
        new List<RejectedEnergyResultMessageRejectReason> { new("E18", "Det virker ikke!") },
        TransactionId.From("4E85A732-85FD-4D92-8FF3-72C052802717"));

    private static readonly Period _period = new(
        Instant.FromUtc(2024, 9, 1, 0, 0),
        Instant.FromUtc(2024, 10, 1, 0, 0));

    private static MessageId _relatedToMessageId = MessageId.Create(Guid.NewGuid().ToString());
    private static ActorRole _receiverRole = ActorRole.MeteredDataResponsible;
    private static ActorNumber _receiverNumber = ActorNumber.Create("1234567890123");

#pragma warning disable CA1822
    public RejectedEnergyResultMessageDto Build()
#pragma warning restore CA1822
    {
        return new RejectedEnergyResultMessageDto(
            _receiverNumber,
            _processId,
            _eventId,
            _businessReason,
            _receiverRole,
            _relatedToMessageId,
            _series,
            _receiverNumber,
            _receiverRole,
            _period);
    }

    public RejectedEnergyResultMessageDtoBuilder WithRelationTo(MessageId relatedToMessageId)
    {
        _relatedToMessageId = relatedToMessageId;
        return this;
    }

    public RejectedEnergyResultMessageDtoBuilder WithReceiverNumber(string receiverNumber)
    {
        _receiverNumber = ActorNumber.Create(receiverNumber);
        return this;
    }

    public RejectedEnergyResultMessageDtoBuilder WithReceiverRole(ActorRole actorRole)
    {
        _receiverRole = actorRole;
        return this;
    }
}
