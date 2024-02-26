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

using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents.Common;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace Energinet.DataHub.EDI.AcceptanceTests.Drivers.MessageFactories;

internal static class AmountPerChargeResultProducedV1Factory
{
    public static AmountPerChargeResultProducedV1 CreateMonthlyAmountPerChargeResultProduced(
        string gridAreaCode,
        string energySupplierId,
        string chargeOwnerId)
    {
        var amountPerChargeResultProduced = new AmountPerChargeResultProducedV1
        {
            CalculationId = Guid.NewGuid().ToString(),
            CalculationType = AmountPerChargeResultProducedV1.Types.CalculationType.WholesaleFixing,
            PeriodStartUtc = DateTime.UtcNow.ToTimestamp(),
            PeriodEndUtc = DateTime.UtcNow.ToTimestamp(),
            GridAreaCode = gridAreaCode,
            EnergySupplierId = energySupplierId,
            ChargeCode = "ESP-C-F-04",
            ChargeType = AmountPerChargeResultProducedV1.Types.ChargeType.Fee,
            ChargeOwnerId = chargeOwnerId,
            QuantityUnit = AmountPerChargeResultProducedV1.Types.QuantityUnit.Kwh,
            IsTax = false,
            Currency = AmountPerChargeResultProducedV1.Types.Currency.Dkk,
            CalculationResultVersion = 42,
        };

        amountPerChargeResultProduced.TimeSeriesPoints.Add(new RepeatedField<AmountPerChargeResultProducedV1.Types.TimeSeriesPoint>()
        {
            new AmountPerChargeResultProducedV1.Types.TimeSeriesPoint()
            {
                QuantityQualities = { AmountPerChargeResultProducedV1.Types.QuantityQuality.Calculated },
                Quantity = new DecimalValue { Nanos = 1, Units = 0 },
                Amount = new DecimalValue { Nanos = 1234, Units = 1234 },
                Price = new DecimalValue { Nanos = 1234, Units = 1234 },
                Time = DateTime.UtcNow.ToTimestamp(),
            },
        });

        return amountPerChargeResultProduced;
    }
}
