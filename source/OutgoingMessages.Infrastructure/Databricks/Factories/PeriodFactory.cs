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
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Models;
using NodaTime;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.Factories;

public static class PeriodFactory
{
    private static readonly DateTimeZone _dkTimeZone = DateTimeZoneProviders.Tzdb["Europe/Copenhagen"];

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
        var calculationEnd = GetEndDateWithResolutionOffset(resolution, timeForNewestPoint);
        return new Period(calculationStart, calculationEnd);
    }

    /// <summary>
    /// In order get the full calculate period, we need to find the oldest and newest point including resolution.
    /// The oldest point is the start of the calculation period.
    /// The newest point plus the resolution is the end of the calculation period.
    /// </summary>
    public static Period GetPeriod(
        IReadOnlyCollection<EnergyTimeSeriesPoint> timeSeriesPoints,
        Resolution resolution)
    {
        var calculationStart = timeSeriesPoints.Min(x => x.TimeUtc);
        var timeForNewestPoint = timeSeriesPoints.Max(x => x.TimeUtc);

        // A period is described by { start: latestPoint.time, end: newestPoint.time + resolution }
        var calculationEnd = GetEndDateWithResolutionOffset(resolution, timeForNewestPoint);
        return new Period(calculationStart, calculationEnd);
    }

    public static Instant GetEndDateWithResolutionOffset(
        Resolution resolution,
        Instant timeForLatestPoint)
    {
        switch (resolution)
        {
            case var res when res == Resolution.QuarterHourly:
                return timeForLatestPoint.Plus(Duration.FromMinutes(15));
            case var res when res == Resolution.Hourly:
                return timeForLatestPoint.Plus(Duration.FromHours(1));
            case var res when res == Resolution.Daily:
                return EnsureMidnight(timeForLatestPoint, daysToAdd: 1);
            case var res when res == Resolution.Monthly:
                return EnsureMidnight(timeForLatestPoint, monthsToAdd: 1);

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(resolution),
                    resolution,
                    "Unknown resolution");
        }
    }

    private static Instant EnsureMidnight(Instant date, int daysToAdd = 0, int monthsToAdd = 0)
    {
        var localDate = date.InZone(_dkTimeZone).LocalDateTime;
        var midnight = localDate
            .PlusMonths(monthsToAdd)
            .PlusDays(daysToAdd).Date
            .AtMidnight();
        return midnight.InZoneStrictly(_dkTimeZone).ToInstant();
    }
}
