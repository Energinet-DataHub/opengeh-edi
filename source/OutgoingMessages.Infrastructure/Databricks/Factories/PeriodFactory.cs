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
    /// The oldest point is the start of the calculation period.
    /// The newest point plus the resolution is the end of the calculation period.
    /// </summary>
    /// <param name="timeSeriesPoints"></param>
    /// <param name="resolution"></param>
    public static Period GetPeriod(IReadOnlyCollection<WholesaleTimeSeriesPoint> timeSeriesPoints, Resolution resolution)
    {
        var calculationStart = timeSeriesPoints.Min(x => x.TimeUtc);
        var timeForNewestPoint = timeSeriesPoints.Max(x => x.TimeUtc);

        // A period is described by { start: latestPoint.time, end: newestPoint.time + resolution }
        var calculationEnd = GetEndDateWithResolutionOffset(resolution, timeForNewestPoint, DateTimeZoneProviders.Tzdb["Europe/Copenhagen"]);
        return new Period(calculationStart, calculationEnd);
    }

    private static Instant GetEndDateWithResolutionOffset(Resolution resolution, Instant timeForLatestPoint, DateTimeZone dateTimeZone)
    {
        switch (resolution)
        {
            case var res when res == Resolution.Hourly:
                return timeForLatestPoint.Plus(Duration.FromHours(1));
            case var res when res == Resolution.Daily:
                return timeForLatestPoint.Plus(Duration.FromDays(1));
            case var res when res == Resolution.Monthly:
                {
                    var timeForLatestPointInLocalTime = timeForLatestPoint.InZone(dateTimeZone).LocalDateTime;
                    var endAtMidnightInLocalTime = timeForLatestPointInLocalTime.PlusMonths(1).Date.AtMidnight();
                    var endAtMidnightInUtc = endAtMidnightInLocalTime.InZoneStrictly(dateTimeZone);
                    return endAtMidnightInUtc.ToInstant();
                }

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(resolution),
                    resolution,
                    "Unknown resolution");
        }
    }
}
