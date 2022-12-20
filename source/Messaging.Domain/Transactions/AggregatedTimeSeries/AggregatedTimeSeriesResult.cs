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

using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages.NotifyAggregatedMeasureData;

namespace Messaging.Domain.Transactions.AggregatedTimeSeries;

public class AggregatedTimeSeriesResult
{
    public AggregatedTimeSeriesResult(Guid id, IReadOnlyList<Series> series)
    {
        Id = id;
        Series = series;
    }

    public Guid Id { get; }

    public IReadOnlyList<Series> Series { get; }
}

public class Series
{
    public Series(IReadOnlyList<Point> points, string gridAreaCode, string meteringPointType, ActorNumber gridOperatorId, string measureUnitType, string resolution)
    {
        Points = points;
        GridAreaCode = gridAreaCode;
        MeteringPointType = meteringPointType;
        GridOperatorId = gridOperatorId;
        MeasureUnitType = measureUnitType;
        Resolution = resolution;
    }

    public IReadOnlyList<Point> Points { get; }

    public string GridAreaCode { get; }

    public string MeteringPointType { get; }

    public string MeasureUnitType { get; }

    public ActorNumber GridOperatorId { get; }

    public string Resolution { get; }
}
