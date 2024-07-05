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
using Microsoft.EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using NodaTime;
using NodaTime.Text;
using Xunit;

namespace Energinet.DataHub.EDI.Tests.Infrastructure.OutgoingMessages.Databricks.Factories;

public class PeriodFactoryTests
{
    [Theory]
    [InlineData("2023-01-31T22:00:00Z")]
    [InlineData("2024-02-29T22:00:00Z")]
    [InlineData("2023-02-28T22:00:00Z")]
    [InlineData("2023-03-31T22:00:00Z")]
    [InlineData("2023-04-30T22:00:00Z")]
    [InlineData("2023-05-31T22:00:00Z")]
    [InlineData("2023-06-30T22:00:00Z")]
    [InlineData("2023-07-31T22:00:00Z")]
    [InlineData("2023-08-31T22:00:00Z")]
    [InlineData("2023-09-30T22:00:00Z")]
    [InlineData("2023-10-31T22:00:00Z")]
    [InlineData("2023-11-30T22:00:00Z")]
    [InlineData("2023-12-31T22:00:00Z")]
    public void Given_TimeSeriesPoint_WhenResolutionIsMonthly_Then_PeriodIsCorrect(string date)
    {
        // Arrange
        var expectedStartDate = InstantPattern.General.Parse(date).Value;
        var instantInNextMonth = expectedStartDate.Plus(Duration.FromDays(1));
        var amountOfDaysInMonth = CalendarSystem.Gregorian.GetDaysInMonth(instantInNextMonth.Year(), instantInNextMonth.Month());
        var expectedEndDate = InstantPattern.General.Parse(date).Value.Plus(Duration.FromDays(amountOfDaysInMonth));

        var points = new List<WholesaleTimeSeriesPoint>
        {
            new(InstantPattern.General.Parse(date).Value, null, new[] { QuantityQuality.Missing }, null, null),
        };

        // Act
        var period = PeriodFactory.GetPeriod(points, Resolution.Monthly);

        // Assert
        period.Should().NotBeNull();
        period.Start.Should().Be(expectedStartDate);
        period.End.Should().Be(expectedEndDate);
    }

    [Theory]
    [InlineData("2023-01-31T22:00:00Z")]
    [InlineData("2024-02-29T22:00:00Z")]
    [InlineData("2023-02-28T22:00:00Z")]
    [InlineData("2023-03-31T22:00:00Z")]
    [InlineData("2023-04-30T22:00:00Z")]
    [InlineData("2023-05-31T22:00:00Z")]
    [InlineData("2023-06-30T22:00:00Z")]
    [InlineData("2023-07-31T22:00:00Z")]
    [InlineData("2023-08-31T22:00:00Z")]
    [InlineData("2023-09-30T22:00:00Z")]
    [InlineData("2023-10-31T22:00:00Z")]
    [InlineData("2023-11-30T22:00:00Z")]
    [InlineData("2023-12-31T22:00:00Z")]
    public void Given_TimeSeriesPoint_WhenResolutionIsDaily_Then_PeriodIsCorrect(string date)
    {
        // Arrange
        var expectedStartDate = InstantPattern.General.Parse(date).Value;
        var expectedEndDate = expectedStartDate.Plus(Duration.FromDays(1));

        var points = new List<WholesaleTimeSeriesPoint>
        {
            new(InstantPattern.General.Parse(date).Value, null, new[] { QuantityQuality.Missing }, null, null),
        };

        // Act
        var period = PeriodFactory.GetPeriod(points, Resolution.Daily);

        // Assert
        period.Should().NotBeNull();
        period.Start.Should().Be(expectedStartDate);
        period.End.Should().Be(expectedEndDate);
    }

    [Theory]
    [InlineData("2023-01-31T22:00:00Z")]
    [InlineData("2023-01-31T23:00:00Z")]
    [InlineData("2023-02-01T13:00:00Z")]
    [InlineData("2023-02-28T13:00:00Z")]
    [InlineData("2023-07-01T13:00:00Z")]
    [InlineData("2023-12-31T22:00:00Z")]
    [InlineData("2024-03-31T01:00:00Z")] //to daylightsavings time
    [InlineData("2024-10-27T00:00:00Z")] //from daylightsavings time
    public void Given_TimeSeriesPoint_WhenResolutionIsHourly_Then_PeriodIsCorrect(string date)
    {
        // Arrange
        var expectedStartDate = InstantPattern.General.Parse(date).Value;
        var expectedEndDate = expectedStartDate.Plus(Duration.FromHours(1));

        var points = new List<WholesaleTimeSeriesPoint>
        {
            new(InstantPattern.General.Parse(date).Value, null, new[] { QuantityQuality.Missing }, null, null),
        };

        // Act
        var period = PeriodFactory.GetPeriod(points, Resolution.Hourly);

        // Assert
        period.Should().NotBeNull();
        period.Start.Should().Be(expectedStartDate);
        period.End.Should().Be(expectedEndDate);
    }
}
