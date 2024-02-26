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

using Energinet.DataHub.EDI.Process.Application.Transactions.Mappers;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Application.Process.Transactions.Mappers;

public class SettlementTypeMapperTests : BaseEnumMapperTests
{
    private readonly EnergyResultProducedV2.Types.TimeSeriesType[] _invalidValues =
    {
        EnergyResultProducedV2.Types.TimeSeriesType.Production,
        EnergyResultProducedV2.Types.TimeSeriesType.NetExchangePerGa,
        EnergyResultProducedV2.Types.TimeSeriesType.NetExchangePerNeighboringGa,
        EnergyResultProducedV2.Types.TimeSeriesType.GridLoss,
        EnergyResultProducedV2.Types.TimeSeriesType.NegativeGridLoss,
        EnergyResultProducedV2.Types.TimeSeriesType.PositiveGridLoss,
        EnergyResultProducedV2.Types.TimeSeriesType.TotalConsumption,
        EnergyResultProducedV2.Types.TimeSeriesType.TempFlexConsumption,
        EnergyResultProducedV2.Types.TimeSeriesType.TempProduction,
    };

    [Theory]
    [MemberData(nameof(GetEnumValues), typeof(EnergyResultProducedV2.Types.TimeSeriesType))]
    public void Ensure_handling_energy_result_produced(EnergyResultProducedV2.Types.TimeSeriesType value)
        => EnsureCanMapOrReturnsNull(
            () => SettlementTypeMapper.Map(value),
            value,
            unspecifiedValue: EnergyResultProducedV2.Types.TimeSeriesType.Unspecified,
            invalidValues: _invalidValues);

    [Theory]
    [MemberData(nameof(GetEnumValues), typeof(AmountPerChargeResultProducedV1.Types.SettlementMethod))]
    public void Ensure_handling_settlement_method_from_amount_per_charge_result_produced(AmountPerChargeResultProducedV1.Types.SettlementMethod value)
        => EnsureCanMapOrReturnsNull(
            () => SettlementTypeMapper.Map(value),
            value,
            unspecifiedValue: AmountPerChargeResultProducedV1.Types.SettlementMethod.Unspecified);
}
