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
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Google.Protobuf.WellKnownTypes;
using NodaTime;
using NodaTime.Serialization.Protobuf;

namespace Energinet.DataHub.EDI.IntegrationTests.Factories;

public class MonthlyAmountPerChargeResultProducedV1EventBuilder
{
    private Guid _calculationId = Guid.NewGuid();
    private MonthlyAmountPerChargeResultProducedV1.Types.CalculationType _calculationType = MonthlyAmountPerChargeResultProducedV1.Types.CalculationType.WholesaleFixing;
    private Timestamp _periodStartUtc = Instant.FromUtc(2023, 10, 1, 0, 0, 0).ToTimestamp();
    private Timestamp _periodEndUtc = Instant.FromUtc(2023, 10, 2, 0, 0, 0).ToTimestamp();
    private string _gridAreaCode = "805";
    private string _energySupplier = "8200000007743";
    private string _chargeCode = "IDontKow";
    private MonthlyAmountPerChargeResultProducedV1.Types.ChargeType _chargeType = MonthlyAmountPerChargeResultProducedV1.Types.ChargeType.Fee;
    private string _chargeOwner = "8200000007740";
    private MonthlyAmountPerChargeResultProducedV1.Types.QuantityUnit _quantityUnit = MonthlyAmountPerChargeResultProducedV1.Types.QuantityUnit.Kwh;
    private bool _isTax;
    private MonthlyAmountPerChargeResultProducedV1.Types.Currency _currency = MonthlyAmountPerChargeResultProducedV1.Types.Currency.Dkk;
    private Energinet.DataHub.Wholesale.Contracts.IntegrationEvents.Common.DecimalValue? _amount;

    internal MonthlyAmountPerChargeResultProducedV1 Build()
    {
        var @event = new MonthlyAmountPerChargeResultProducedV1
        {
            CalculationId = _calculationId.ToString(),
            CalculationType = _calculationType,
            PeriodStartUtc = _periodStartUtc,
            PeriodEndUtc = _periodEndUtc,
            GridAreaCode = _gridAreaCode,
            EnergySupplierId = _energySupplier,
            ChargeCode = _chargeCode,
            ChargeType = _chargeType,
            ChargeOwnerId = _chargeOwner,
            QuantityUnit = _quantityUnit,
            IsTax = _isTax,
            Currency = _currency,
        };
        if (_amount != null)
            @event.Amount = _amount;

        return @event;
    }

    internal MonthlyAmountPerChargeResultProducedV1EventBuilder WithCalculationType(MonthlyAmountPerChargeResultProducedV1.Types.CalculationType calculationType)
    {
        _calculationType = calculationType;
        return this;
    }

    internal MonthlyAmountPerChargeResultProducedV1EventBuilder WithStartOfPeriod(Timestamp startOfPeriod)
    {
        _periodStartUtc = startOfPeriod;
        return this;
    }

    internal MonthlyAmountPerChargeResultProducedV1EventBuilder WithEndOfPeriod(Timestamp endOfPeriod)
    {
        _periodEndUtc = endOfPeriod;
        return this;
    }

    internal MonthlyAmountPerChargeResultProducedV1EventBuilder WithGridAreaCode(string gridAreaCode)
    {
        _gridAreaCode = gridAreaCode;
        return this;
    }

    internal MonthlyAmountPerChargeResultProducedV1EventBuilder WithEnergySupplier(string energySupplier)
    {
        _energySupplier = energySupplier;
        return this;
    }

    internal MonthlyAmountPerChargeResultProducedV1EventBuilder WithChargeCode(string chargeCode)
    {
        _chargeCode = chargeCode;
        return this;
    }

    internal MonthlyAmountPerChargeResultProducedV1EventBuilder WithChargeType(MonthlyAmountPerChargeResultProducedV1.Types.ChargeType chargeType)
    {
        _chargeType = chargeType;
        return this;
    }

    internal MonthlyAmountPerChargeResultProducedV1EventBuilder WithChargeOwner(string chargeOwner)
    {
        _chargeOwner = chargeOwner;
        return this;
    }

    internal MonthlyAmountPerChargeResultProducedV1EventBuilder WithQuantityUnit(MonthlyAmountPerChargeResultProducedV1.Types.QuantityUnit quantityUnit)
    {
        _quantityUnit = quantityUnit;
        return this;
    }

    internal MonthlyAmountPerChargeResultProducedV1EventBuilder WithIsTax(bool isTax)
    {
        _isTax = isTax;
        return this;
    }

    internal MonthlyAmountPerChargeResultProducedV1EventBuilder WithCurrency(MonthlyAmountPerChargeResultProducedV1.Types.Currency currency)
    {
        _currency = currency;
        return this;
    }

    internal MonthlyAmountPerChargeResultProducedV1EventBuilder WithAmount(Energinet.DataHub.Wholesale.Contracts.IntegrationEvents.Common.DecimalValue? amount)
    {
        _amount = amount;
        return this;
    }

    internal MonthlyAmountPerChargeResultProducedV1EventBuilder WithCalculationId(Guid calculationId)
    {
        _calculationId = calculationId;
        return this;
    }
}
