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

using Energinet.DataHub.EDI.Process.Application.Transactions.Mappers;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Application.Process.Transactions.Mappers;

public class CurrencyMapperTests : BaseEnumMapperTests
{
    [Theory]
    [MemberData(nameof(GetEnumValues), typeof(MonthlyAmountPerChargeResultProducedV1.Types.Currency))]
    public void Ensure_handling_monthly_amount_per_charge_result_produced(MonthlyAmountPerChargeResultProducedV1.Types.Currency value)
        => EnsureCanMapOrThrows(
            () => CurrencyMapper.Map(value),
            value,
            unspecifiedValue: MonthlyAmountPerChargeResultProducedV1.Types.Currency.Unspecified);

    [Theory]
    [MemberData(nameof(GetEnumValues), typeof(AmountPerChargeResultProducedV1.Types.Currency))]
    public void Ensure_handling_amount_per_charge_result_produced(AmountPerChargeResultProducedV1.Types.Currency value)
        => EnsureCanMapOrThrows(
            () => CurrencyMapper.Map(value),
            value,
            unspecifiedValue: AmountPerChargeResultProducedV1.Types.Currency.Unspecified);
}
