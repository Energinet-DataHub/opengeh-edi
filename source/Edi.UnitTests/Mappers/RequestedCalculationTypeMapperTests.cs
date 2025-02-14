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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.Wholesale.Edi.Mappers;
using Energinet.DataHub.Wholesale.Edi.Models;
using FluentAssertions;
using Xunit;

namespace Energinet.DataHub.Wholesale.Edi.UnitTests.Mappers;

public class RequestedCalculationTypeMapperTests
{
    public static IEnumerable<object[]> InvalidBusinessReasonAndSettlementVersionData()
    {
        yield return [BusinessReason.BalanceFixing.Name, DataHubNames.SettlementVersion.FirstCorrection];
        yield return [BusinessReason.PreliminaryAggregation.Name, "random-string"];
        yield return [BusinessReason.WholesaleFixing.Name, DataHubNames.SettlementVersion.FirstCorrection];
        yield return [BusinessReason.Correction.Name, string.Empty];
        yield return [BusinessReason.Correction.Name, "random-string"];
        yield return [string.Empty, string.Empty];
        yield return ["random-string", string.Empty];
        yield return ["random-string", DataHubNames.SettlementVersion.FirstCorrection];
    }

    public static IEnumerable<object[]> ValidBusinessReasonAndSettlementVersionData()
    {
        yield return [BusinessReason.BalanceFixing.Name, null!, RequestedCalculationType.BalanceFixing];
        yield return [BusinessReason.PreliminaryAggregation.Name, null!, RequestedCalculationType.PreliminaryAggregation];
        yield return [BusinessReason.WholesaleFixing.Name, null!, RequestedCalculationType.WholesaleFixing];
        yield return [BusinessReason.Correction.Name, DataHubNames.SettlementVersion.FirstCorrection, RequestedCalculationType.FirstCorrection];
        yield return [BusinessReason.Correction.Name, DataHubNames.SettlementVersion.SecondCorrection, RequestedCalculationType.SecondCorrection];
        yield return [BusinessReason.Correction.Name, DataHubNames.SettlementVersion.ThirdCorrection, RequestedCalculationType.ThirdCorrection];
        yield return [BusinessReason.Correction.Name, null!, RequestedCalculationType.LatestCorrection];
    }

    [Theory]
    [MemberData(nameof(ValidBusinessReasonAndSettlementVersionData))]
    public void ToRequestedCalculationType_WhenValidBusinessReasonAndSettlementVersion_ReturnsExpectedType(string businessReason, string? settlementVersion, RequestedCalculationType expectedType)
    {
        // Act
        var actualType = RequestedCalculationTypeMapper.ToRequestedCalculationType(businessReason, settlementVersion);

        // Assert
        actualType.Should().Be(expectedType);
    }

    [Theory]
    [MemberData(nameof(InvalidBusinessReasonAndSettlementVersionData))]
    public void ToRequestedCalculationType_WhenInvalidBusinessReasonAndSettlementVersionCombination_ThrowsArgumentOutOfRangeException(string businessReason, string? settlementVersion)
    {
        // Act
        var act = () => RequestedCalculationTypeMapper.ToRequestedCalculationType(businessReason, settlementVersion);

        // Assert
        act.Should().ThrowExactly<ArgumentOutOfRangeException>().And.ActualValue.Should().Be(settlementVersion);
    }

    [Theory]
    [InlineData("", null)]
    [InlineData("random-string", null)]
    public void ToRequestedCalculationType_WhenInvalidBusinessReason_ThrowsArgumentOutOfRangeException(string businessReason, string? settlementVersion)
    {
        // Act
        var act = () => RequestedCalculationTypeMapper.ToRequestedCalculationType(businessReason, settlementVersion);

        // Assert
        act.Should().ThrowExactly<ArgumentOutOfRangeException>().And.ActualValue.Should().Be(businessReason);
    }
}
