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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.Wholesale.Edi.Mappers;
using FluentAssertions;
using Xunit;

namespace Energinet.DataHub.Wholesale.Edi.UnitTests.Mappers;

public class ChargeTypeMapperTests
{
    public static IEnumerable<object[]> GetChargeTypeTestData()
    {
        yield return [ChargeType.Tariff.Name, DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults.ChargeType.Tariff];
        yield return [ChargeType.Fee.Name, DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults.ChargeType.Fee];
        yield return [ChargeType.Subscription.Name, DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults.ChargeType.Subscription];
    }

    [Theory]
    [MemberData(nameof(GetChargeTypeTestData))]
    public void Map_WhenValid_ReturnsExpectedChargeType(string chargeType, DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults.ChargeType expectedResult)
    {
        // Act
        var actual = ChargeTypeMapper.Map(chargeType);

        // Assert
        actual.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData("InvalidChargeType")]
    public void Map_WhenInvalidRequestedCalculationType_ThrowsArgumentOutOfRangeException(string chargeType)
    {
        // Act
        var act = () => ChargeTypeMapper.Map(chargeType);

        // Assert
        act.Should().ThrowExactly<ArgumentOutOfRangeException>().And.ActualValue.Should().Be(chargeType);
    }
}
