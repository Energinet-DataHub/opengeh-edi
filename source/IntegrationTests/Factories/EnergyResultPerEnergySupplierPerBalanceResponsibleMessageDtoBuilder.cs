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

public class EnergyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder
{
    private readonly EventId _eventId = EventId.From(Guid.NewGuid());
    private readonly Guid _calculationId = Guid.NewGuid();

    private BusinessReason _businessReason = BusinessReason.BalanceFixing;
    private ActorNumber _receiverNumber = ActorNumber.Create("1234567891912");
    private ActorNumber _balanceResponsibleNumber = ActorNumber.Create("1234567891911");
    private Guid _calculationResultId;

    public EnergyResultPerEnergySupplierPerBalanceResponsibleMessageDto Build()
    {
        return new EnergyResultPerEnergySupplierPerBalanceResponsibleMessageDto(
            eventId: _eventId,
            businessReason: _businessReason,
            gridArea: "805",
            meteringPointType: MeteringPointType.Consumption,
            settlementMethod: SettlementMethod.NonProfiled,
            measurementUnit: MeasurementUnit.Pieces,
            resolution: Resolution.Hourly,
            energySupplierNumber: _receiverNumber,
            balanceResponsibleNumber: _balanceResponsibleNumber,
            period: new Period(DateTimeOffset.UtcNow.ToInstant(), DateTimeOffset.UtcNow.AddHours(1).ToInstant()),
            points: new List<EnergyResultMessagePoint>
            {
                new(1, 1, CalculatedQuantityQuality.Calculated, DateTimeOffset.UtcNow.ToInstant().ToString()),
            },
            calculationResultVersion: 1,
            settlementVersion: null,
            calculationResultId: _calculationResultId,
            calculationId: _calculationId);
    }

    public EnergyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder WithCalculationResultId(Guid calculationResultId)
    {
        _calculationResultId = calculationResultId;
        return this;
    }

    public EnergyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder WithEnergySupplierReceiverNumber(string receiverIdValue)
    {
        _receiverNumber = ActorNumber.Create(receiverIdValue);
        return this;
    }

    public EnergyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder WithBalanceResponsiblePartyReceiverNumber(string receiverIdValue)
    {
        _balanceResponsibleNumber = ActorNumber.Create(receiverIdValue);
        return this;
    }

    public EnergyResultPerEnergySupplierPerBalanceResponsibleMessageDtoBuilder WithBusinessReason(BusinessReason businessReason)
    {
        _businessReason = businessReason;
        return this;
    }
}
