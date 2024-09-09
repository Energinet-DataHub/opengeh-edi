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

using Xunit;

namespace Energinet.DataHub.EDI.Tests.Application.Process.Transactions.Mappers;

public class MeteringPointTypeMapperTests : BaseEnumMapperTests
{
    [Theory]
    [MemberData(nameof(GetEnumValues), typeof(WholesaleServicesRequestSeries.Types.MeteringPointType))]
    public void Given_WholesaleServiceMeteringPointType_When_Mapping_Then_HandlesExceptedValues(
        WholesaleServicesRequestSeries.Types.MeteringPointType value)
        => EnsureCanMapOrThrows(
            () => Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices.Mappers
                .MeteringPointTypeMapper.Map(value),
            value,
            unspecifiedValue: WholesaleServicesRequestSeries.Types.MeteringPointType.Unspecified);
}
