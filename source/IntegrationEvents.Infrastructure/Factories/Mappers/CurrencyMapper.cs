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

public static class CurrencyMapper
{
    public static Currency Map(MonthlyAmountPerChargeResultProducedV1.Types.Currency currency)
    {
        return currency switch
        {
            MonthlyAmountPerChargeResultProducedV1.Types.Currency.Dkk => Currency.DanishCrowns,
            MonthlyAmountPerChargeResultProducedV1.Types.Currency.Unspecified => throw new InvalidOperationException("Currency is not specified from Wholesale"),
            _ => throw new ArgumentOutOfRangeException(nameof(currency), currency, "Unknown currency from Wholesale"),
        };
    }

    public static Currency Map(AmountPerChargeResultProducedV1.Types.Currency currency)
    {
        return currency switch
        {
            AmountPerChargeResultProducedV1.Types.Currency.Dkk => Currency.DanishCrowns,
            AmountPerChargeResultProducedV1.Types.Currency.Unspecified => throw new InvalidOperationException("Currency is not specified from Wholesale"),
            _ => throw new ArgumentOutOfRangeException(nameof(currency), currency, "Unknown currency from Wholesale"),
        };
    }

    public static Currency Map(TotalMonthlyAmountResultProducedV1.Types.Currency currency)
    {
        return currency switch
        {
            TotalMonthlyAmountResultProducedV1.Types.Currency.Dkk => Currency.DanishCrowns,
            TotalMonthlyAmountResultProducedV1.Types.Currency.Unspecified => throw new InvalidOperationException("Currency is not specified from Wholesale"),
            _ => throw new ArgumentOutOfRangeException(nameof(currency), currency, "Unknown currency from Wholesale"),
        };
    }
}
