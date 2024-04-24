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

using System.Linq;
using Energinet.DataHub.Edi.Requests;
using Energinet.DataHub.Edi.Responses;
using Google.Protobuf.WellKnownTypes;
using Period = Energinet.DataHub.Edi.Responses.Period;

namespace Energinet.DataHub.EDI.IntegrationTests.EventBuilders;

internal static class AggregatedTimeSeriesRequestAcceptedEventBuilder
{
    public static AggregatedTimeSeriesRequestAccepted BuildEventFrom(
        AggregatedTimeSeriesRequest aggregatedTimeSeriesRequest)
    {
        var @event = new AggregatedTimeSeriesRequestAccepted();

        var series = aggregatedTimeSeriesRequest.GridAreaCodes
            .Select(gridArea => new Series
            {
                GridArea = gridArea,
                QuantityUnit = QuantityUnit.Kwh,
                TimeSeriesType = TimeSeriesType.Production,
                Resolution = Resolution.Pt1H,
                CalculationResultVersion = 1024,
                Period = new Period
                {
                    StartOfPeriod = new Timestamp { Seconds = 512, Nanos = 256 },
                    EndOfPeriod = new Timestamp { Seconds = 1024, Nanos = 512 },
                },
                TimeSeriesPoints =
                {
                    new TimeSeriesPoint
                    {
                        Quantity = new DecimalValue { Units = 32, Nanos = 64 },
                        Time = new Timestamp { Seconds = 128, Nanos = 256 },
                        QuantityQualities = { QuantityQuality.Calculated, QuantityQuality.Measured },
                    },
                },
            });

        @event.Series.Add(series);

        return @event;
    }
}
