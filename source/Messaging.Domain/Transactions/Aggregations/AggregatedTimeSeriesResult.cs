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

using Messaging.Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using NodaTime;

namespace Messaging.Domain.Transactions.Aggregations;

public class AggregatedTimeSeriesResult
{
    public AggregatedTimeSeriesResult(Guid id, IReadOnlyList<AggregationResult> series)
    {
        Id = id;
        Series = series;
    }

    public Guid Id { get; }

    public IReadOnlyList<AggregationResult> Series { get; }
}

public class AggregationResult
{
    public AggregationResult(Guid id, IReadOnlyList<Point> points, string gridAreaCode, string meteringPointType, string measureUnitType, string resolution)
    {
        Id = id;
        Points = points;
        GridAreaCode = gridAreaCode;
        MeteringPointType = meteringPointType;
        MeasureUnitType = measureUnitType;
        Resolution = resolution;
    }

    public Guid Id { get; }

    public IReadOnlyList<Point> Points { get; }

    public string GridAreaCode { get; }

    public string MeteringPointType { get; }

    public string MeasureUnitType { get; }

    public string Resolution { get; }
}
