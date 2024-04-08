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
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using NodaTime.Extensions;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace Energinet.DataHub.EDI.IntegrationTests.Factories;

public class EnergyResultMessageDtoBuilder
{
    private readonly Guid _processId = ProcessId.Create(Guid.NewGuid()).Id;
    private readonly IReadOnlyCollection<EnergyResultMessagePoint> _points = new List<EnergyResultMessagePoint>();
    private BusinessReason _businessReason = BusinessReason.BalanceFixing;
    private SettlementVersion? _settlementVersion;
    private ActorNumber _receiverNumber = ActorNumber.Create("1234567891912");
    private ActorRole _receiverRole = ActorRole.MeteredDataAdministrator;
    private string _gridAreaCode = "805";

#pragma warning disable CA1822
    public EnergyResultMessageDto Build()
#pragma warning restore CA1822
    {
        return EnergyResultMessageDto.Create(
            _receiverNumber,
            _receiverRole,
            _processId,
            _gridAreaCode,
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

    public EnergyResultMessageDtoBuilder WithGridAreaCode(string gridAreaCode)
    {
        _gridAreaCode = gridAreaCode;
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
}
