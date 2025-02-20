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

using System.Reflection;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Mappers.EnergyResults;
using FluentAssertions;

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Infrastructure.Databricks.DeltaTableMappers.EnergyResults;

public class ResolutionMapperTests
{
    public static TheoryData<string, Resolution> GetResolutionTestData() =>
        new()
        {
            { "PT1H", Resolution.Hourly },
            { "PT15M", Resolution.QuarterHourly },
        };

    public static TheoryData<Resolution> GetInvalidResolutions()
    {
        var invalidResolutions = new TheoryData<Resolution>();

        var allResolutions =
            typeof(Resolution).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

        var resolutions = allResolutions.Select(f => f.GetValue(null)).Cast<Resolution>();
        var validResolutions = GetResolutionTestData().Select(x => x[1]);
        foreach (var invalidResolution in resolutions.Where(res => !validResolutions.Contains(res)))
        {
            invalidResolutions.Add(invalidResolution);
        }

        return invalidResolutions;
    }

    [Theory]
    [MemberData(nameof(GetResolutionTestData))]
    public void Given_Resolution_When_MappingToAndFromDeltaTableValue_Then_ReturnsExpectedValue(string deltaTableValue, Resolution resolution)
    {
        // Act
        var actualResolution = ResolutionMapper.FromDeltaTableValue(deltaTableValue);
        var actualDeltaTableValue = ResolutionMapper.ToDeltaTableValue(resolution);

        // Assert
        actualResolution.Should().Be(resolution);
        actualDeltaTableValue.Should().Be(deltaTableValue);
    }

    [Fact]
    public void Given_InvalidDeltaTableValue_When_InvalidDeltaTableValue_Then_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalidDeltaTableValue = Guid.NewGuid().ToString();

        // Act
        var act = () => ResolutionMapper.FromDeltaTableValue(invalidDeltaTableValue);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ActualValue.Should().Be(invalidDeltaTableValue);
    }

    [Theory]
    [MemberData(nameof(GetInvalidResolutions))]
    public void Given_InvalidResolutionDeltaTableValue_When_InvalidDeltaTableValue_Then_ThrowsArgumentOutOfRangeException(Resolution invalidResolution)
    {
        // Act
        var act = () => ResolutionMapper.ToDeltaTableValue(invalidResolution);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .And.ActualValue.Should().Be(invalidResolution);
    }
}
