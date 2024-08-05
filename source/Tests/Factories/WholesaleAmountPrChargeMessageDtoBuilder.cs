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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using NodaTime.Extensions;

namespace Energinet.DataHub.EDI.Tests.Factories;

public class WholesaleAmountPrChargeMessageDtoBuilder
{
    private readonly ActorNumber _chargeOwner = DataHubDetails.DataHubActorNumber;
    private readonly BusinessReason _businessReason = BusinessReason.BalanceFixing;
    private readonly EventId _eventId = EventId.From(Guid.NewGuid());
    private readonly Guid _calculationId = Guid.NewGuid();
    private ActorNumber _receiverNumber = ActorNumber.Create("1234567890123");

    public WholesaleAmountPerChargeMessageDto Build()
    {
        var series = new WholesaleServicesSeriesBuilder().BuildWholesaleCalculation().Points;

        return new WholesaleAmountPerChargeMessageDto(
            eventId: _eventId,
            calculationId: _calculationId,
            calculationResultId: Guid.NewGuid(),
            calculationResultVersion: 1,
            energySupplierReceiverId: _receiverNumber,
            chargeOwnerReceiverId: _chargeOwner,
            chargeOwnerId: _chargeOwner,
            businessReason: _businessReason.Name,
            gridAreaCode: "805",
            isTax: false,
            period: new Period(DateTimeOffset.UtcNow.ToInstant(), DateTimeOffset.UtcNow.AddHours(1).ToInstant()),
            quantityUnit: MeasurementUnit.Kwh,
            currency: Currency.DanishCrowns,
            chargeType: ChargeType.Fee,
            resolution: Resolution.Hourly,
            settlementVersion: null,
            meteringPointType: MeteringPointType.Consumption,
            settlementMethod: null,
            chargeCode: "78554884",
            points: series);
    }

    public WholesaleAmountPrChargeMessageDtoBuilder WithReceiverNumber(ActorNumber number)
    {
        _receiverNumber = number;
        return this;
    }
}
