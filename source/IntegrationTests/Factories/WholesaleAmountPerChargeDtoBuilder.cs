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
using NodaTime.Extensions;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace Energinet.DataHub.EDI.IntegrationTests.Factories;

public class WholesaleAmountPerChargeDtoBuilder
{
    private readonly Guid _processId = Guid.NewGuid();
    private EventId _eventId = EventId.From(Guid.NewGuid());
    private BusinessReason _businessReason = BusinessReason.WholesaleFixing;
    private ActorNumber _receiverNumber = ActorNumber.Create("1234567891912");
    private ActorRole _receiverRole = ActorRole.EnergySupplier;
    private ActorNumber _chargeOwnerId = ActorNumber.Create("1234567891911");
    private ActorRole _chargeOwnerRole = ActorRole.GridOperator;
    private Guid _calculationResultId;

    public WholesaleAmountPerChargeDto Build()
    {
        return new WholesaleAmountPerChargeDto(
            _eventId,
            _calculationResultId,
            1,
            _receiverNumber,
            _chargeOwnerId,
            _chargeOwnerId,
            BusinessReason.WholesaleFixing.ToString(),
            "805",
            false,
            new Period(DateTimeOffset.UtcNow.ToInstant(), DateTimeOffset.UtcNow.AddHours(1).ToInstant()),
            MeasurementUnit.Pieces,
            Currency.DanishCrowns,
            ChargeType.Tariff,
            Resolution.Hourly,
            null,
            MeteringPointType.Consumption,
            SettlementMethod.NonProfiled,
            "1234567891911",
            new List<WholesaleServicesPoint>
            {
                new(1, 10, 100, 5, CalculatedQuantityQuality.Estimated),
            });
    }

    public WholesaleAmountPerChargeDtoBuilder WithEventId(EventId eventId)
    {
        _eventId = eventId;
        return this;
    }

    public WholesaleAmountPerChargeDtoBuilder WithBusinessReason(BusinessReason businessReason)
    {
        _businessReason = businessReason;
        return this;
    }

    public WholesaleAmountPerChargeDtoBuilder WithCalculationResultId(Guid calculationResultId)
    {
        _calculationResultId = calculationResultId;
        return this;
    }

    public WholesaleAmountPerChargeDtoBuilder WithReceiverNumber(string receiverIdValue)
    {
        _receiverNumber = ActorNumber.Create(receiverIdValue);
        return this;
    }

    public WholesaleAmountPerChargeDtoBuilder WithReceiverRole(ActorRole receiverRole)
    {
        _receiverRole = receiverRole;
        return this;
    }
}
