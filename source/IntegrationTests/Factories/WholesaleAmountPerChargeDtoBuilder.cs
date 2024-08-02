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
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.WholesaleResultMessages;
using NodaTime.Extensions;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace Energinet.DataHub.EDI.IntegrationTests.Factories;

public class WholesaleAmountPerChargeDtoBuilder
{
    private readonly EventId _eventId = EventId.From(Guid.NewGuid());
    private readonly Guid _calculationId = Guid.NewGuid();
    private readonly BusinessReason _businessReason = BusinessReason.WholesaleFixing;

    private ActorNumber _chargeOwnerActorNumber = ActorNumber.Create("1234567891911");
    private ActorNumber _receiverActorNumber = ActorNumber.Create("1234567891912");
    private Guid _calculationResultId;

    public WholesaleAmountPerChargeMessageDto Build()
    {
        return new WholesaleAmountPerChargeMessageDto(
            eventId: _eventId,
            calculationId: _calculationId,
            calculationResultId: _calculationResultId,
            calculationResultVersion: 1,
            energySupplierReceiverId: _receiverActorNumber,
            chargeOwnerReceiverId: _chargeOwnerActorNumber,
            chargeOwnerId: _chargeOwnerActorNumber,
            businessReason: _businessReason.ToString(),
            gridAreaCode: "805",
            isTax: false,
            period: new Period(DateTimeOffset.UtcNow.ToInstant(), DateTimeOffset.UtcNow.AddHours(1).ToInstant()),
            quantityUnit: MeasurementUnit.Pieces,
            currency: Currency.DanishCrowns,
            chargeType: ChargeType.Tariff,
            resolution: Resolution.Hourly,
            settlementVersion: null,
            meteringPointType: MeteringPointType.Consumption,
            settlementMethod: SettlementMethod.NonProfiled,
            chargeCode: "1234567891911",
            points:
            [
                new(1, 10, 100, 5, CalculatedQuantityQuality.Estimated),
            ]);
    }

    public WholesaleAmountPerChargeDtoBuilder WithCalculationResultId(Guid calculationResultId)
    {
        _calculationResultId = calculationResultId;
        return this;
    }

    public WholesaleAmountPerChargeDtoBuilder WithReceiverNumber(ActorNumber receiverActorNumber)
    {
        _receiverActorNumber = receiverActorNumber;
        return this;
    }

    public WholesaleAmountPerChargeDtoBuilder WithChargeOwnerNumber(ActorNumber chargeOwnerActorNumber)
    {
        _chargeOwnerActorNumber = chargeOwnerActorNumber;
        return this;
    }
}
