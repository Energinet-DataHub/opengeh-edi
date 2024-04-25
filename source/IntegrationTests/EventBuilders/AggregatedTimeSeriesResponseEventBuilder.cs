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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Energinet.DataHub.Edi.Requests;
using Energinet.DataHub.Edi.Responses;
using Google.Protobuf.WellKnownTypes;
using NodaTime;
using NodaTime.Serialization.Protobuf;
using NodaTime.Text;
using Duration = NodaTime.Duration;
using Period = Energinet.DataHub.Edi.Responses.Period;

namespace Energinet.DataHub.EDI.IntegrationTests.EventBuilders;

[SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "Random not used for security")]
internal static class AggregatedTimeSeriesResponseEventBuilder
{
    public static AggregatedTimeSeriesRequestAccepted GenerateAcceptedFrom(
        AggregatedTimeSeriesRequest request, Instant now, IReadOnlyCollection<string>? defaultGridAreas = null)
    {
        var @event = new AggregatedTimeSeriesRequestAccepted();

        if (request.GridAreaCodes.Count == 0 && defaultGridAreas == null)
            throw new ArgumentNullException(nameof(defaultGridAreas), "defaultGridAreas must be set when request has no GridAreaCodes");

        var gridAreas = request.GridAreaCodes.ToList();
        if (gridAreas.Count == 0)
            gridAreas.AddRange(defaultGridAreas!);

        var series = gridAreas
            .Select(gridArea =>
            {
                var periodStart = InstantPattern.General.Parse(request.Period.Start).Value;
                var periodEnd = InstantPattern.General.Parse(request.Period.End).Value;

                var timeSeriesType = request.SettlementMethod switch
                {
                    BuildingBlocks.Domain.DataHub.DataHubNames.SettlementMethod.Flex => TimeSeriesType.FlexConsumption,
                    BuildingBlocks.Domain.DataHub.DataHubNames.SettlementMethod.NonProfiled => TimeSeriesType.NonProfiledConsumption,
                    _ => throw new NotImplementedException($"Unknown SettlementMethod: {request.SettlementMethod}"),
                };
                var resolution = Resolution.Pt1H;
                var points = CreatePoints(resolution, periodStart, periodEnd);

                return new Series
                {
                    GridArea = gridArea,
                    QuantityUnit = QuantityUnit.Kwh,
                    TimeSeriesType = timeSeriesType,
                    Resolution = resolution,
                    CalculationResultVersion = now.ToUnixTimeTicks(),
                    Period = new Period
                    {
                        StartOfPeriod = periodStart.ToTimestamp(),
                        EndOfPeriod = periodEnd.ToTimestamp(),
                    },
                    TimeSeriesPoints = { points },
                };
            });

        @event.Series.Add(series);

        return @event;
    }

    private static List<TimeSeriesPoint> CreatePoints(Resolution resolution, Instant periodStart, Instant periodEnd)
    {
        var resolutionDuration = resolution switch
        {
            Resolution.Pt1H => Duration.FromHours(1),
            Resolution.Pt15M => Duration.FromMinutes(15),
            _ => throw new NotImplementedException($"Unsupported resolution in request: {resolution}"),
        };

        var points = new List<TimeSeriesPoint>();
        var currentTime = periodStart;
        while (currentTime < periodEnd)
        {
            points.Add(CreatePoint(currentTime, quantityFactor: resolution == Resolution.Pt1H ? 4 : 1));
            currentTime = currentTime.Plus(resolutionDuration);
        }

        return points;
    }

    private static TimeSeriesPoint CreatePoint(Instant currentTime, int quantityFactor = 1)
    {
        // Create random quantity between 1.00 and 999.999 (multiplied a factor used by by monthly resolution)
        var quantity = new DecimalValue { Units = Random.Shared.Next(1, 999) * quantityFactor, Nanos = Random.Shared.Next(0, 999) };

        return new TimeSeriesPoint
        {
            Time = currentTime.ToTimestamp(),
            Quantity = quantity,
            QuantityQualities = { QuantityQuality.Estimated },
        };
    }
}
