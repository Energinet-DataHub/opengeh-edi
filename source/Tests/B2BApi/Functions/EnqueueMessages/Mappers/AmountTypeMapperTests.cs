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

using Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages.Mappers;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults;
using Energinet.DataHub.ProcessManager.Orchestrations.Abstractions.Processes.BRS_026_028.BRS_028.V1.Model;
using Xunit;
using Resolution = Energinet.DataHub.ProcessManager.Components.Abstractions.ValueObjects.Resolution;

namespace Energinet.DataHub.EDI.Tests.B2BApi.Functions.EnqueueMessages.Mappers;

public class AmountTypeMapperTests
{
    public static IEnumerable<object[]> InvalidResolutions =>
        new List<object[]>
        {
            new object[] { Resolution.Hourly },
            new object[] { Resolution.Daily },
            new object[] { Resolution.QuarterHourly },
        };

    [Fact]
    public void Map_ShouldReturnAmountPerCharge_WhenResolutionIsNull()
    {
        // Arrange
        Resolution? resolution = null;
        var chargeTypes = new List<RequestCalculatedWholesaleServicesAcceptedV1.AcceptedChargeType>();

        // Act
        var result = AmountTypeMapper.Map(resolution, chargeTypes);

        // Assert
        Assert.Single(result);
        Assert.Contains(AmountType.AmountPerCharge, result);
    }

    [Fact]
    public void Map_ShouldReturnMonthlyAmountPerChargeAndTotalMonthlyAmount_WhenResolutionIsMonthlyAndNoChargeTypes()
    {
        // Arrange
        var resolution = Resolution.Monthly;
        var chargeTypes = new List<RequestCalculatedWholesaleServicesAcceptedV1.AcceptedChargeType>();

        // Act
        var result = AmountTypeMapper.Map(resolution, chargeTypes);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(AmountType.MonthlyAmountPerCharge, result);
        Assert.Contains(AmountType.TotalMonthlyAmount, result);
    }

    [Fact]
    public void Map_ShouldReturnMonthlyAmountPerCharge_WhenResolutionIsMonthlyAndHasChargeTypes()
    {
        // Arrange
        var resolution = Resolution.Monthly;
        var chargeTypes = new List<RequestCalculatedWholesaleServicesAcceptedV1.AcceptedChargeType>
        {
            new(null, string.Empty),
        };

        // Act
        var result = AmountTypeMapper.Map(resolution, chargeTypes);

        // Assert
        Assert.Single(result);
        Assert.Contains(AmountType.MonthlyAmountPerCharge, result);
    }

    [Theory]
    [MemberData(nameof(InvalidResolutions))]
    public void Map_ShouldThrowArgumentOutOfRangeException_WhenResolutionIsInvalid(Resolution resolution)
    {
        // Arrange
        var chargeTypes = new List<RequestCalculatedWholesaleServicesAcceptedV1.AcceptedChargeType>();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => AmountTypeMapper.Map(resolution, chargeTypes));
    }
}
