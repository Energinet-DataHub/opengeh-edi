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
using Google.Protobuf.WellKnownTypes;

namespace Energinet.DataHub.EDI.AcceptanceTests.Drivers.MessageFactories;

internal static class MonthlyAmountPerChargeResultProducedV1Factory
{
    public static MonthlyAmountPerChargeResultProducedV1 CreateMonthlyAmountPerChargeResultProduced(
        string gridAreaCode,
        string energySupplierId,
        string chargeOwnerId)
    {
        var monthlyAmountPerChargeResultProduced = new MonthlyAmountPerChargeResultProducedV1
        {
            CalculationId = Guid.NewGuid().ToString(),
            CalculationType = MonthlyAmountPerChargeResultProducedV1.Types.CalculationType.WholesaleFixing,
            PeriodStartUtc = DateTime.UtcNow.ToTimestamp(),
            PeriodEndUtc = DateTime.UtcNow.ToTimestamp(),
            GridAreaCode = gridAreaCode,
            EnergySupplierId = energySupplierId,
            ChargeCode = "ESP-C-F-04",
            ChargeType = MonthlyAmountPerChargeResultProducedV1.Types.ChargeType.Fee,
            ChargeOwnerId = chargeOwnerId,
            QuantityUnit = MonthlyAmountPerChargeResultProducedV1.Types.QuantityUnit.Kwh,
            IsTax = false,
            Currency = MonthlyAmountPerChargeResultProducedV1.Types.Currency.Dkk,
            Amount = new DecimalValue { Nanos = 1234, Units = 1234 },
            CalculationResultVersion = 42,
        };

        return monthlyAmountPerChargeResultProduced;
    }
}
