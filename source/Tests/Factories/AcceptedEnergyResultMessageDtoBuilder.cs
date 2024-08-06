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
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.EnergyResultMessages.Request;
using NodaTime.Extensions;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace Energinet.DataHub.EDI.Tests.Factories;

public class AcceptedEnergyResultMessageDtoBuilder
{
    private const string GridAreaCode = "805";
    private readonly IReadOnlyCollection<AcceptedEnergyResultMessagePoint> _points = [];
    private readonly Guid _processId = Guid.NewGuid();
    private readonly ActorRole _documentReceiverRole = ActorRole.MeteredDataResponsible;
    private readonly ActorNumber _documentReceiverNumber = ActorNumber.Create("1234567891913");
    private EventId _eventId = EventId.From(Guid.NewGuid());
    private BusinessReason _businessReason = BusinessReason.BalanceFixing;
    private SettlementVersion? _settlementVersion;
    private ActorNumber _receiverNumber = ActorNumber.Create("1234567891912");
    private ActorRole _receiverRole = ActorRole.MeteredDataAdministrator;

    public AcceptedEnergyResultMessageDto Build()
    {
        return AcceptedEnergyResultMessageDto.Create(
            _receiverNumber,
            _receiverRole,
            _documentReceiverNumber,
            _documentReceiverRole,
            _processId,
            _eventId,
            GridAreaCode,
            MeteringPointType.Consumption.Name,
            SettlementMethod.NonProfiled.Name,
            MeasurementUnit.Kwh.Name,
            Resolution.Hourly.Name,
            _receiverNumber.Value,
            null,
            new Period(DateTimeOffset.UtcNow.ToInstant(), DateTimeOffset.UtcNow.AddHours(1).ToInstant()),
            _points,
            _businessReason.Name,
            1,
            TransactionId.From("1234567891912"),
            _settlementVersion?.Name,
            null);
    }

    public AcceptedEnergyResultMessageDtoBuilder WithReceiverNumber(string receiverNumber)
    {
        _receiverNumber = ActorNumber.Create(receiverNumber);
        return this;
    }

    public AcceptedEnergyResultMessageDtoBuilder WithReceiverRole(ActorRole actorRole)
    {
        _receiverRole = actorRole;
        return this;
    }

    public AcceptedEnergyResultMessageDtoBuilder WithBusinessReason(BusinessReason businessReason)
    {
        _businessReason = businessReason;
        return this;
    }

    public AcceptedEnergyResultMessageDtoBuilder WithSettlementVersion(SettlementVersion settlementVersion)
    {
        _settlementVersion = settlementVersion;
        return this;
    }

    public AcceptedEnergyResultMessageDtoBuilder WithEventId(EventId eventId)
    {
        _eventId = eventId;
        return this;
    }
}
