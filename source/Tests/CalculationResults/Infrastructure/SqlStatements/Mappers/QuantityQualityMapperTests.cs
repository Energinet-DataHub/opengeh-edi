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

using Energinet.DataHub.Core.TestCommon.AutoFixture.Attributes;
using Energinet.DataHub.Wholesale.CalculationResults.Infrastructure.SqlStatements.Mappers;
using Energinet.DataHub.Wholesale.CalculationResults.Interfaces.CalculationResults.Model;
using FluentAssertions;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.CalculationResults.Infrastructure.SqlStatements.Mappers;

public class QuantityQualityMapperTests
{
    [Theory]
    [InlineAutoMoqData("calculated", QuantityQuality.Calculated)]
    [InlineAutoMoqData("estimated", QuantityQuality.Estimated)]
    [InlineAutoMoqData("measured", QuantityQuality.Measured)]
    [InlineAutoMoqData("missing", QuantityQuality.Missing)]
    public void FromDeltaTableValue_ReturnsValidQuantityQuality(string deltaValue, QuantityQuality? expected)
    {
        // Act
        var actual = QuantityQualityMapper.FromDeltaTableValue(deltaValue);

        // Assert
        actual.Should().Be(expected);
    }

    [Fact]
    public void FromDeltaTableValue_WhenInvalidDeltaTableValue_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalidDeltaTableValue = Guid.NewGuid().ToString();

        // Act
        var act = () => QuantityQualityMapper.FromDeltaTableValue(invalidDeltaTableValue);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ActualValue.Should().Be(invalidDeltaTableValue);
    }
}
