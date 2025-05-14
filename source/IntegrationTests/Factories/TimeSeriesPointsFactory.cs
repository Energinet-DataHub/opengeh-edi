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
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.WholesaleResultMessages;
using Energinet.DataHub.EDI.OutgoingMessages.UnitTests.Domain.Asserts;
using NodaTime;
using Period = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Period;
using Resolution = Energinet.DataHub.EDI.BuildingBlocks.Domain.Models.Resolution;

namespace Energinet.DataHub.EDI.IntegrationTests.Factories;

internal static class TimeSeriesPointsFactory
{
    public static IReadOnlyCollection<TimeSeriesPointAssertionInput> CreatePointsForDay(
        Instant start,
        decimal quantity,
        CalculatedQuantityQuality calculatedQuality)
    {
        var points = new List<TimeSeriesPointAssertionInput>();
        for (var i = 0; i < 24; i++)
        {
            points.Add(new TimeSeriesPointAssertionInput(
                start.Plus(Duration.FromHours(i)),
                quantity,
                calculatedQuality));
        }

        return points;
    }

    public static IReadOnlyCollection<WholesaleServicesPoint> CreatePointsForPeriod(
        Period period,
        Resolution resolution,
        decimal? price,
        decimal? quantity,
        decimal amount,
        CalculatedQuantityQuality? calculatedQuality)
    {
        var points = new List<WholesaleServicesPoint>();
        var posistion = 1;

        var currentTime = period.Start.ToDateTimeOffset();
        while (currentTime < period.End.ToDateTimeOffset())
        {
            points.Add(new WholesaleServicesPoint(posistion, quantity, price, amount, calculatedQuality));
            posistion++;
            currentTime = GetDateTimeWithResolutionOffset(resolution, currentTime);
        }

        return points;
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
                    "Unknown databricks resolution");
        }
    }
}
