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

using Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Factories.Mappers;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Application.Process.Transactions.Mappers;

public class SettlementVersionMapperTests : BaseEnumMapperTests
{
    private readonly EnergyResultProducedV2.Types.CalculationType[] _energyResultProducedInvalidValues =
    {
        EnergyResultProducedV2.Types.CalculationType.Aggregation,
        EnergyResultProducedV2.Types.CalculationType.BalanceFixing,
        EnergyResultProducedV2.Types.CalculationType.WholesaleFixing,
    };

    private readonly MonthlyAmountPerChargeResultProducedV1.Types.CalculationType[] _monthlyAmountPerChargeResultInvalidValues =
    {
        MonthlyAmountPerChargeResultProducedV1.Types.CalculationType.WholesaleFixing,
    };

    private readonly AmountPerChargeResultProducedV1.Types.CalculationType[] _amountPerChargeResultInvalidValues =
    {
        AmountPerChargeResultProducedV1.Types.CalculationType.WholesaleFixing,
    };

    private readonly TotalMonthlyAmountResultProducedV1.Types.CalculationType[] _totalMonthlyAmountResultInvalidValues =
    {
        TotalMonthlyAmountResultProducedV1.Types.CalculationType.WholesaleFixing,
    };

    [Theory]
    [MemberData(nameof(GetEnumValues), typeof(EnergyResultProducedV2.Types.CalculationType))]
    public void Ensure_handling_energy_result_produced(EnergyResultProducedV2.Types.CalculationType value)
        => EnsureCanMapOrReturnsNull(
            () => SettlementVersionMapper.Map(value),
            value,
            unspecifiedValue: EnergyResultProducedV2.Types.CalculationType.Unspecified,
            invalidValues: _energyResultProducedInvalidValues);

    [Theory]
    [MemberData(nameof(GetEnumValues), typeof(MonthlyAmountPerChargeResultProducedV1.Types.CalculationType))]
    public void Ensure_handling_monthly_amount_per_charge_result_produced(MonthlyAmountPerChargeResultProducedV1.Types.CalculationType value)
        => EnsureCanMapOrReturnsNull(
            () => SettlementVersionMapper.Map(value),
            value,
            unspecifiedValue: MonthlyAmountPerChargeResultProducedV1.Types.CalculationType.Unspecified,
            invalidValues: _monthlyAmountPerChargeResultInvalidValues);

    [Theory]
    [MemberData(nameof(GetEnumValues), typeof(AmountPerChargeResultProducedV1.Types.CalculationType))]
    public void Ensure_handling__amount_per_charge_result_produced(AmountPerChargeResultProducedV1.Types.CalculationType value)
        => EnsureCanMapOrReturnsNull(
            () => SettlementVersionMapper.Map(value),
            value,
            unspecifiedValue: AmountPerChargeResultProducedV1.Types.CalculationType.Unspecified,
            invalidValues: _amountPerChargeResultInvalidValues);

    [Theory]
    [MemberData(nameof(GetEnumValues), typeof(TotalMonthlyAmountResultProducedV1.Types.CalculationType))]
    public void Ensure_handling__total_monthly_amount_result_produced(TotalMonthlyAmountResultProducedV1.Types.CalculationType value)
        => EnsureCanMapOrReturnsNull(
            () => SettlementVersionMapper.Map(value),
            value,
            unspecifiedValue: TotalMonthlyAmountResultProducedV1.Types.CalculationType.Unspecified,
            invalidValues: _totalMonthlyAmountResultInvalidValues);
}
