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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Models;
using Microsoft.EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using NodaTime;
using NodaTime.Extensions;
using NodaTime.Text;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.Factories;

public static class PeriodFactory
{
    /// <summary>
    /// In order get the full calculate period, we need to find the oldest and newest point including resolution.
    /// Since timeSeriesPoints measurements period is represented by the start time and a resolution.
    /// </summary>
    /// <param name="timeSeriesPoints"></param>
    /// <param name="resolution"></param>
    public static Period GetPeriod(IReadOnlyCollection<WholesaleTimeSeriesPoint> timeSeriesPoints, Resolution resolution)
    {
        var timeForOldestPoint = timeSeriesPoints.Min(x => x.TimeUtc);
        var timeForLatestPoint = timeSeriesPoints.Max(x => x.TimeUtc);

        // A point represents a measurement in a period { start: point.time, end: point.time + resolution }
        // Since the time is the start of the measured period, we need to add the resolution to get the end of the calculation period.
        var endDateWithResolutionOffset = GetEndDateWithResolutionOffset(resolution, timeForLatestPoint);
        return new Period(timeForOldestPoint, endDateWithResolutionOffset);
    }

    private static Instant GetEndDateWithResolutionOffset(Resolution resolution, Instant timeForLatestPoint)
    {
        switch (resolution)
        {
            case var res when res == Resolution.Hourly:
                return timeForLatestPoint.Plus(Duration.FromHours(1));
            case var res when res == Resolution.Daily:
                return timeForLatestPoint.Plus(Duration.FromDays(1));
            case var res when res == Resolution.Monthly:
                {
                    // A monthly calculation is executed for a month represented in danish time
                    //  E.g. Calculation start: 2023-02-01T00:00:00Z (danish time)
                    // When this is converted to UTC, it will be the last day of the previous month.
                    // E.g. Calculation start: 2023-01-31T22:00:00Z (utc time)
                    // By adding a day, we get the next month, which is the calculated month.
                    var instantInNextMonth = timeForLatestPoint.Plus(Duration.FromDays(1));
                    var days = CalendarSystem.Gregorian.GetDaysInMonth(instantInNextMonth.Year(), instantInNextMonth.Month());
                    return timeForLatestPoint.Plus(Duration.FromDays(days));
                }

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(resolution),
                    resolution,
                    "Unknown resolution");
        }
    }
}
