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

using System.Collections.Generic;
using System.Linq;
using Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Exceptions;
using Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Factories.Mappers;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using FluentAssertions;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Application.Process.Transactions.Mappers;

public class MeteringPointTypeMapperTests : BaseEnumMapperTests
{
    private static readonly AmountPerChargeResultProducedV1.Types.MeteringPointType[] _invalidValues =
    {
        AmountPerChargeResultProducedV1.Types.MeteringPointType.VeProduction,
        AmountPerChargeResultProducedV1.Types.MeteringPointType.NetProduction,
        AmountPerChargeResultProducedV1.Types.MeteringPointType.SupplyToGrid,
        AmountPerChargeResultProducedV1.Types.MeteringPointType.ConsumptionFromGrid,
        AmountPerChargeResultProducedV1.Types.MeteringPointType.WholesaleServicesInformation,
        AmountPerChargeResultProducedV1.Types.MeteringPointType.OwnProduction,
        AmountPerChargeResultProducedV1.Types.MeteringPointType.NetFromGrid,
        AmountPerChargeResultProducedV1.Types.MeteringPointType.NetToGrid,
        AmountPerChargeResultProducedV1.Types.MeteringPointType.TotalConsumption,
        AmountPerChargeResultProducedV1.Types.MeteringPointType.ElectricalHeating,
        AmountPerChargeResultProducedV1.Types.MeteringPointType.NetConsumption,
        AmountPerChargeResultProducedV1.Types.MeteringPointType.EffectSettlement,
    };

    public static IEnumerable<object[]> GetInvalidAmountPerChargeResultProducedV1MeteringPointTypes()
    {
        return _invalidValues.Select(document => new object[] { document }).ToList();
    }

    [Theory]
    [MemberData(nameof(GetEnumValues), typeof(EnergyResultProducedV2.Types.TimeSeriesType))]
    public void Ensure_handling_energy_result_produced(EnergyResultProducedV2.Types.TimeSeriesType value)
        => EnsureCanMapOrThrows(
            () => MeteringPointTypeMapper.Map(value),
            value,
            unspecifiedValue: EnergyResultProducedV2.Types.TimeSeriesType.Unspecified);

    [Theory]
    [MemberData(nameof(GetEnumValues), typeof(AmountPerChargeResultProducedV1.Types.MeteringPointType))]
    public void Ensure_handling_metering_point_type_from_amount_per_charge_result_produced(
        AmountPerChargeResultProducedV1.Types.MeteringPointType value)
        => EnsureCanMapOrThrows(
            () => MeteringPointTypeMapper.Map(value),
            value,
            unspecifiedValue: AmountPerChargeResultProducedV1.Types.MeteringPointType.Unspecified,
            invalidValues: _invalidValues);

    [Theory]
    [MemberData(nameof(GetInvalidAmountPerChargeResultProducedV1MeteringPointTypes))]
    public void Ensure_throws_domain_exception(AmountPerChargeResultProducedV1.Types.MeteringPointType value)
    {
        var act = () => MeteringPointTypeMapper.Map(value);

        act.Should().ThrowExactly<NotSupportedMeteringPointTypeException>();
    }
}
