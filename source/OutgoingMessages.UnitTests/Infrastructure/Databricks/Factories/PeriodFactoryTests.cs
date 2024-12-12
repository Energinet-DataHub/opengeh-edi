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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.Factories;
using FluentAssertions;
using NodaTime.Text;

namespace Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Infrastructure.Databricks.Factories;

public class PeriodFactoryTests
{
    [Theory]

    // From summer time to winter time
    [InlineData("2021-10-30T22:00:00Z", nameof(Resolution.Daily), "2021-10-31T23:00:00Z")]
    [InlineData("2021-09-30T22:00:00Z", nameof(Resolution.Monthly), "2021-10-31T23:00:00Z")]

    // From winter time to summer time
    [InlineData("2024-03-30T23:00:00Z", nameof(Resolution.Daily), "2024-03-31T22:00:00Z")]
    [InlineData("2024-02-29T23:00:00Z", nameof(Resolution.Monthly), "2024-03-31T22:00:00Z")]
    public void Given_SummerWinterTimeChangeDate_When_Mapping_Then_ReturnsExpectedDateWithSummerWinterTimeCorrection(string date, string resolution, string expected)
    {
        // Arrange
        var dateAsInstant = InstantPattern.ExtendedIso.Parse(date).Value;
        var domainResolution = Resolution.FromName(resolution);
        var expectedDate = InstantPattern.ExtendedIso.Parse(expected).Value;

        // Act
        var actual = PeriodFactory.GetEndDateWithResolutionOffset(domainResolution, dateAsInstant);

        // Assert
        actual.Should().Be(expectedDate);
    }

    [Theory]
    [InlineData("2021-10-26T22:00:00Z", nameof(Resolution.Daily), "2021-10-27T22:00:00Z")]
    [InlineData("2021-07-31T22:00:00Z", nameof(Resolution.Monthly), "2021-08-31T22:00:00Z")]

    // From summer time to winter time
    [InlineData("2021-10-31T02:00:00Z", nameof(Resolution.QuarterHourly), "2021-10-31T02:15:00Z")]
    [InlineData("2021-10-31T02:00:00Z", nameof(Resolution.Hourly), "2021-10-31T03:00:00Z")]

    // From winter time to summer time
    [InlineData("2024-03-31T03:00:00Z", nameof(Resolution.QuarterHourly), "2024-03-31T03:15:00Z")]
    [InlineData("2024-03-31T03:00:00Z", nameof(Resolution.Hourly), "2024-03-31T04:00:00Z")]
    public void Given_DatesWithoutSummerWinterTimeChange_When_Mapping_Then_ReturnsExpectedDateWithNoCorrection(string date, string resolution, string expected)
    {
        // Arrange
        var dateAsInstant = InstantPattern.ExtendedIso.Parse(date).Value;
        var domainResolution = Resolution.FromName(resolution);
        var expectedDate = InstantPattern.ExtendedIso.Parse(expected).Value;

        // Act
        var actual = PeriodFactory.GetEndDateWithResolutionOffset(domainResolution, dateAsInstant);

        // Assert
        actual.Should().Be(expectedDate);
    }
}
