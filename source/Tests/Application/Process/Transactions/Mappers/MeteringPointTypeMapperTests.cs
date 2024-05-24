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
using Energinet.DataHub.Edi.Responses;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Application.Process.Transactions.Mappers;

public class MeteringPointTypeMapperTests : BaseEnumMapperTests
{
    private readonly EnergyResultProducedV2.Types.TimeSeriesType[] _invalidValues =
    {
        EnergyResultProducedV2.Types.TimeSeriesType.NetExchangePerNeighboringGa,
    };

    [Theory]
    [MemberData(nameof(GetEnumValues), typeof(EnergyResultProducedV2.Types.TimeSeriesType))]
    public void Ensure_handling_energy_result_produced(EnergyResultProducedV2.Types.TimeSeriesType value)
        => EnsureCanMapOrThrows(
            () => MeteringPointTypeMapper.Map(value),
            value,
            unspecifiedValue: EnergyResultProducedV2.Types.TimeSeriesType.Unspecified,
            invalidValues: _invalidValues);

    [Theory]
    [MemberData(nameof(GetEnumValues), typeof(AmountPerChargeResultProducedV1.Types.MeteringPointType))]
    public void Ensure_handling_metering_point_type_from_amount_per_charge_result_produced(
        AmountPerChargeResultProducedV1.Types.MeteringPointType value)
        => EnsureCanMapOrThrows(
            () => MeteringPointTypeMapper.Map(value),
            value,
            unspecifiedValue: AmountPerChargeResultProducedV1.Types.MeteringPointType.Unspecified);

    [Theory]
    [MemberData(nameof(GetEnumValues), typeof(WholesaleServicesRequestSeries.Types.MeteringPointType))]
    public void Given_WholesaleServiceMeteringPointType_When_Mapping_Then_HandlesExceptedValues(
        WholesaleServicesRequestSeries.Types.MeteringPointType value)
        => EnsureCanMapOrReturnsNull(
            () => Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices.Mappers.MeteringPointTypeMapper.Map(value),
            value);
}
