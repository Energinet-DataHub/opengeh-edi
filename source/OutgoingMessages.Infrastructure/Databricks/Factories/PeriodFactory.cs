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
using NodaTime.Extensions;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.Factories;

public static class PeriodFactory
{
    public static Period GetPeriod(IReadOnlyCollection<WholesaleTimeSeriesPoint> timeSeriesPoints, Resolution resolution)
    {
        var start = timeSeriesPoints.Min(x => x.TimeUtc);
        var end = timeSeriesPoints.Max(x => x.TimeUtc);
        // The end date is the start of the next period.
        var endWithResolutionOffset = GetDateTimeWithResolutionOffset(resolution, end.ToDateTimeOffset());
        return new Period(start, endWithResolutionOffset.ToInstant());
    }

    private static DateTimeOffset GetDateTimeWithResolutionOffset(Resolution resolution, DateTimeOffset dateTime)
    {
        switch (resolution)
        {
            case var res when res == Resolution.Hourly:
                return dateTime.AddMinutes(60);
            case var res when res == Resolution.Daily:
                return dateTime.AddDays(1);
            case var res when res == Resolution.Monthly:
                return dateTime.AddMonths(1);
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(resolution),
                    resolution,
                    "Unknown resolution");
        }
    }
}
