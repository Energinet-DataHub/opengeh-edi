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

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Messaging.Infrastructure.Transactions.AggregatedTimeSeries;

public record AggregatedTimeSeriesResultsDto(IEnumerable<AggregatedTimeSeriesResultDto> Results);

public record AggregatedTimeSeriesResultDto(string GridAreaCode, string GridOperatorNumber, string MeteringPointType, string MeasureUnitType, string Resolution, IEnumerable<Point> Points);

public class Point
{
    public Point(int position, string quantity, string? quality, string quarterTime)
    {
        Position = position;
        Quantity = quantity;
        Quality = quality;
        QuarterTime = quarterTime;
    }

    [JsonPropertyName("position")]
    public int Position { get; }

    [JsonPropertyName("quantity")]
    public string Quantity { get; }

    [JsonPropertyName("quality")]
    public string? Quality { get; }

    [JsonPropertyName("quarter_time")]
    public string QuarterTime { get; }
}
