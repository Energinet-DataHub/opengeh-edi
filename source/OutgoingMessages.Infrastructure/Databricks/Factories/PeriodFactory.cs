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
    public static Period GetPeriod(IReadOnlyCollection<WholesaleTimeSeriesPoint> timeSeriesPoints, Resolution resolution)
    {
        var start = timeSeriesPoints.Min(x => x.TimeUtc);
        var end = timeSeriesPoints.Max(x => x.TimeUtc);
        // The end date is the start of the next period.
        var endDateWithResolutionOffset = GetEndDateWithResolutionOffset(resolution, end);
        return new Period(start, endDateWithResolutionOffset);
    }

    private static Instant GetEndDateWithResolutionOffset(Resolution resolution, Instant instant)
    {
        switch (resolution)
        {
            case var res when res == Resolution.Hourly:
                return instant.Plus(Duration.FromHours(1));
            case var res when res == Resolution.Daily:
                return instant.Plus(Duration.FromDays(1));
            case var res when res == Resolution.Monthly:
                {
                    var instantInNextMonth = instant.Plus(Duration.FromDays(1));
                    var days = CalendarSystem.Gregorian.GetDaysInMonth(instantInNextMonth.Year(), instantInNextMonth.Month());
                    return instant.Plus(Duration.FromDays(days));
                }

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(resolution),
                    resolution,
                    "Unknown resolution");
        }
    }
}
