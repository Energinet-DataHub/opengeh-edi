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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.DeltaTableConstants;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.Factories;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Models;
using FluentAssertions;
using NodaTime;
using NodaTime.Text;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Databricks.Factories;

public class PeriodFactoryTests
{
    [Fact]
    public void Given_TimeSeriesPoint_WhenResolutionIsMonthly_Then_PeriodIsCorrect()
    {
        // Arrange
        var expectedStartDate = InstantPattern.General.Parse("2023-01-31T23:00:00Z").Value;
        var expectedEndDate = InstantPattern.General.Parse("2023-02-28T23:00:00Z").Value;

        var pointForAMonthlyResolution = new List<WholesaleTimeSeriesPoint>
        {
            new(InstantPattern.General.Parse("2023-01-31T23:00:00Z").Value, null, new[] { QuantityQuality.Missing }, null, null),
        };

        // Act
        var period = PeriodFactory.GetPeriod(pointForAMonthlyResolution, Resolution.Monthly);

        // Assert
        period.Should().NotBeNull();
        period.Start.Should().Be(expectedStartDate);
        period.End.Should().Be(expectedEndDate);
    }

    [Fact]
    public void Given_TimeSeriesPoint_WhenResolutionIsMonthlyAndMonthIsToDaylightSwitchMonth_Then_PeriodIsCorrect()
    {
        // Arrange
        var expectedStartDate = InstantPattern.General.Parse("2023-02-28T23:00:00Z").Value;
        var expectedEndDate = InstantPattern.General.Parse("2023-03-31T22:00:00Z").Value;

        var pointForAMonthlyResolution = new List<WholesaleTimeSeriesPoint>
        {
            new(InstantPattern.General.Parse("2023-02-28T23:00:00Z").Value, null, new[] { QuantityQuality.Missing }, null, null),
        };

        // Act
        var period = PeriodFactory.GetPeriod(pointForAMonthlyResolution, Resolution.Monthly);

        // Assert
        period.Should().NotBeNull();
        period.Start.Should().Be(expectedStartDate);
        period.End.Should().Be(expectedEndDate);
    }

    [Fact]
    public void Given_TimeSeriesPoint_WhenResolutionIsMonthlyAndMonthIsFromDaylightSwitchMonth_Then_PeriodIsCorrect()
    {
        // Arrange
        var expectedStartDate = InstantPattern.General.Parse("2023-09-30T22:00:00Z").Value;
        var expectedEndDate = InstantPattern.General.Parse("2023-10-31T23:00:00Z").Value;

        var pointForAMonthlyResolution = new List<WholesaleTimeSeriesPoint>
        {
            new(InstantPattern.General.Parse("2023-09-30T22:00:00Z").Value, null, new[] { QuantityQuality.Missing }, null, null),
        };

        // Act
        var period = PeriodFactory.GetPeriod(pointForAMonthlyResolution, Resolution.Monthly);

        // Assert
        period.Should().NotBeNull();
        period.Start.Should().Be(expectedStartDate);
        period.End.Should().Be(expectedEndDate);
    }

    [Fact]
    public void Given_TimeSeriesPoint_WhenResolutionIsDaily_Then_PeriodIsCorrect()
    {
        // Arrange
        var expectedStartDate = InstantPattern.General.Parse("2023-01-31T23:00:00Z").Value;
        var expectedEndDate = InstantPattern.General.Parse("2023-02-28T23:00:00Z").Value;

        var points = new List<WholesaleTimeSeriesPoint>();
        var currentTime = InstantPattern.General.Parse("2023-01-31T23:00:00Z").Value;
        // 28 days in February
        while (points.Count < 28)
        {
            points.Add(new WholesaleTimeSeriesPoint(currentTime, null, new[] { QuantityQuality.Missing }, null, null));
            currentTime = currentTime.Plus(Duration.FromDays(1));
        }

        // Act
        var period = PeriodFactory.GetPeriod(points, Resolution.Daily);

        // Assert
        period.Should().NotBeNull();
        period.Start.Should().Be(expectedStartDate);
        period.End.Should().Be(expectedEndDate);
    }

    [Fact]
    public void Given_TimeSeriesPoint_WhenResolutionIsHourly_Then_PeriodIsCorrect()
    {
        // Arrange
        var expectedStartDate = InstantPattern.General.Parse("2023-01-31T23:00:00Z").Value;
        var expectedEndDate = InstantPattern.General.Parse("2023-02-01T23:00:00Z").Value;

        var points = new List<WholesaleTimeSeriesPoint>();
        var currentTime = InstantPattern.General.Parse("2023-01-31T23:00:00Z").Value;
        // 24 hours in a day
        while (points.Count < 24)
        {
            points.Add(new WholesaleTimeSeriesPoint(currentTime, null, new[] { QuantityQuality.Missing }, null, null));
            currentTime = currentTime.Plus(Duration.FromHours(1));
        }

        // Act
        var period = PeriodFactory.GetPeriod(points, Resolution.Hourly);

        // Assert
        period.Should().NotBeNull();
        period.Start.Should().Be(expectedStartDate);
        period.End.Should().Be(expectedEndDate);
    }
}
