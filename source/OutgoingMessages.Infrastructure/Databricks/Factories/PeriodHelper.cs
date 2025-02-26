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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.EnergyResults;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.WholesaleResults;
using NodaTime;
using NodaTime.Extensions;
using InternalPeriod = Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.CalculationResults.Period;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.Factories;

public static class PeriodHelper
{
    private static readonly DateTimeZone _dkTimeZone = DateTimeZoneProviders.Tzdb["Europe/Copenhagen"];

    public static (Instant Start, Instant End) GetPeriod(IReadOnlyCollection<EnergyTimeSeriesPoint> timeSeriesPoints, Resolution resolution)
    {
        var start = timeSeriesPoints.Min(x => x.Time);
        var end = timeSeriesPoints.Max(x => x.Time);
        // The end date is the start of the next period.
        var endWithResolutionOffset = GetDateTimeWithResolutionOffsetForEnergy(resolution, end);
        return (start.ToInstant(), endWithResolutionOffset.ToInstant());
    }

    public static InternalPeriod GetPeriod(IReadOnlyCollection<WholesaleTimeSeriesPoint> timeSeriesPoints, Resolution resolution)
    {
        var start = timeSeriesPoints.Min(x => x.Time);
        var end = timeSeriesPoints.Max(x => x.Time);
        // The end date is the start of the next period.
        var endWithResolutionOffset = GetDateTimeWithResolutionOffset(resolution, end);
        return new InternalPeriod(start.ToInstant(), endWithResolutionOffset.ToInstant());
    }

    public static DateTimeOffset GetDateTimeWithResolutionOffsetForEnergy(Resolution resolution, DateTimeOffset dateTime) => resolution switch
    {
        var res when res == Resolution.QuarterHourly => dateTime.AddMinutes(15),
        _ => dateTime.AddMinutes(60),
    };

    public static DateTimeOffset GetDateTimeWithResolutionOffset(Resolution resolution, DateTimeOffset dateTime) => resolution switch
    {
        var res when res == Resolution.Hourly => dateTime.AddMinutes(60),
        var res when res == Resolution.Daily => EnsureMidnight(dateTime.ToInstant(), daysToAdd: 1),
        _ => EnsureMidnight(dateTime.ToInstant(), monthsToAdd: 1),
    };

    private static DateTimeOffset EnsureMidnight(this Instant dateTime, int daysToAdd = 0, int monthsToAdd = 0)
    {
        var localDate = dateTime.InZone(_dkTimeZone).LocalDateTime;
        var midnight = localDate
            .PlusMonths(monthsToAdd)
            .PlusDays(daysToAdd).Date
            .AtMidnight();
        return midnight.InZoneStrictly(_dkTimeZone).ToDateTimeOffset();
    }
}
