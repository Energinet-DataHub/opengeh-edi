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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;

namespace Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Factories.Mappers;

public static class ChargeTypeMapper
{
    public static ChargeType Map(MonthlyAmountPerChargeResultProducedV1.Types.ChargeType chargeType)
    {
        return chargeType switch
        {
            MonthlyAmountPerChargeResultProducedV1.Types.ChargeType.Fee => ChargeType.Fee,
            MonthlyAmountPerChargeResultProducedV1.Types.ChargeType.Tariff => ChargeType.Tariff,
            MonthlyAmountPerChargeResultProducedV1.Types.ChargeType.Subscription => ChargeType.Subscription,
            MonthlyAmountPerChargeResultProducedV1.Types.ChargeType.Unspecified => throw new InvalidOperationException("Charge type is not specified from Wholesale"),
            _ => throw new ArgumentOutOfRangeException(nameof(chargeType), chargeType, "Unknown charge type from Wholesale"),
        };
    }

    public static ChargeType Map(AmountPerChargeResultProducedV1.Types.ChargeType chargeType)
    {
        return chargeType switch
        {
            AmountPerChargeResultProducedV1.Types.ChargeType.Fee => ChargeType.Fee,
            AmountPerChargeResultProducedV1.Types.ChargeType.Tariff => ChargeType.Tariff,
            AmountPerChargeResultProducedV1.Types.ChargeType.Subscription => ChargeType.Subscription,
            AmountPerChargeResultProducedV1.Types.ChargeType.Unspecified => throw new InvalidOperationException("Charge type is not specified from Wholesale"),
            _ => throw new ArgumentOutOfRangeException(nameof(chargeType), chargeType, "Unknown charge type from Wholesale"),
        };
    }
}
