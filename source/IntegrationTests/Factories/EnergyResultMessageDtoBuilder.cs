﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using NodaTime.Extensions;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace Energinet.DataHub.EDI.IntegrationTests.Factories;

public class EnergyResultMessageDtoBuilder
{
    private const string GridAreaCode = "805";
    private readonly IReadOnlyCollection<EnergyResultMessagePoint> _points = [];
    private readonly Guid _calculationId = Guid.NewGuid();
    private EventId _eventId = EventId.From(Guid.NewGuid());
    private BusinessReason _businessReason = BusinessReason.BalanceFixing;
    private SettlementVersion? _settlementVersion;
    private ActorNumber _receiverNumber = ActorNumber.Create("1234567891912");
    private ActorRole _receiverRole = ActorRole.MeteredDataAdministrator;

    public EnergyResultMessageDto Build()
    {
        return EnergyResultMessageDto.Create(
            _eventId,
            _calculationId,
            _receiverNumber,
            _receiverRole,
            GridAreaCode,
            MeteringPointType.Consumption.Name,
            SettlementMethod.NonProfiled.Name,
            MeasurementUnit.Kwh.Name,
            Resolution.Hourly.Name,
            null,
            "1234567891911",
            new Period(DateTimeOffset.UtcNow.ToInstant(), DateTimeOffset.UtcNow.AddHours(1).ToInstant()),
            _points,
            _businessReason.Name,
            1,
            _settlementVersion?.Name);
    }

    public EnergyResultMessageDtoBuilder WithReceiverNumber(string receiverNumber)
    {
        _receiverNumber = ActorNumber.Create(receiverNumber);
        return this;
    }

    public EnergyResultMessageDtoBuilder WithReceiverRole(ActorRole actorRole)
    {
        _receiverRole = actorRole;
        return this;
    }

    public EnergyResultMessageDtoBuilder WithBusinessReason(BusinessReason businessReason)
    {
        _businessReason = businessReason;
        return this;
    }

    public EnergyResultMessageDtoBuilder WithSettlementVersion(SettlementVersion settlementVersion)
    {
        _settlementVersion = settlementVersion;
        return this;
    }

    public EnergyResultMessageDtoBuilder WithEventId(EventId eventId)
    {
        _eventId = eventId;
        return this;
    }
}
