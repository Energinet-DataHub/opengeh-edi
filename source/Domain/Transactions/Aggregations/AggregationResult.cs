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

using Domain.Actors;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.NotifyAggregatedMeasureData;

namespace Domain.Transactions.Aggregations;

public class AggregationResult
{
    private AggregationResult(Guid id, IReadOnlyList<Point> points, GridArea gridArea, MeteringPointType meteringPointType, MeasurementUnit measureUnitType, Resolution resolution, Period period)
    {
        Id = id;
        Points = points;
        GridArea = gridArea;
        MeteringPointType = meteringPointType;
        MeasureUnitType = measureUnitType;
        Resolution = resolution;
        Period = period;
    }

    private AggregationResult(Guid id, IReadOnlyList<Point> points, GridArea gridArea, MeteringPointType meteringPointType, MeasurementUnit measureUnitType, Resolution resolution, Period period, SettlementType settlementType, ActorNumber? aggregatedForActor = null)
    {
        Id = id;
        Points = points;
        GridArea = gridArea;
        MeteringPointType = meteringPointType;
        MeasureUnitType = measureUnitType;
        Resolution = resolution;
        Period = period;
        SettlementType = settlementType;
        AggregatedForActor = aggregatedForActor;
    }

    public Guid Id { get; }

    public IReadOnlyList<Point> Points { get; }

    public GridArea GridArea { get; }

    public MeteringPointType MeteringPointType { get; }

    public MeasurementUnit MeasureUnitType { get; }

    public Resolution Resolution { get; }

    public Period Period { get; }

    public SettlementType? SettlementType { get; }

    public ActorNumber? AggregatedForActor { get; }

    public static AggregationResult Consumption(
        Guid id,
        GridArea gridAreaCode,
        SettlementType settlementType,
        MeasurementUnit measureUnitType,
        Resolution resolution,
        Period period,
        IReadOnlyList<Point> points,
        ActorNumber? aggregatedForActor = null)
    {
        return new AggregationResult(
            id,
            points,
            gridAreaCode,
            MeteringPointType.Consumption,
            measureUnitType,
            resolution,
            period,
            settlementType,
            aggregatedForActor);
    }

    public static AggregationResult Production(
        Guid id,
        GridArea gridAreaCode,
        MeasurementUnit measurementUnitType,
        Resolution resolution,
        Period period,
        IReadOnlyList<Point> points)
    {
        return new AggregationResult(
            id,
            points,
            gridAreaCode,
            MeteringPointType.Production,
            measurementUnitType,
            resolution,
            period);
    }
}
