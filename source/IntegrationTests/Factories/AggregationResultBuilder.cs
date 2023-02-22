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
using System.Runtime.CompilerServices;
using Domain.Actors;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Domain.SeedWork;
using Domain.Transactions;
using Domain.Transactions.Aggregations;
using NodaTime;
using Period = Domain.Transactions.Aggregations.Period;

namespace IntegrationTests.Factories;

internal class AggregationResultBuilder
{
    private static readonly IReadOnlyList<Point> _points = new List<Point>()
    {
        new(1, 1.1m, Quality.Missing, "2022-10-31T21:15:00.000Z"),
    };

    private readonly MeteringPointType _meteringPointType = MeteringPointType.Consumption;
    private readonly MeasurementUnit _measureUnit = MeasurementUnit.Kwh;
    private readonly SettlementType? _settlementType = SettlementType.NonProfiled;
    private readonly ActorNumber _aggregatedForActor = default!;
    private GridArea _gridArea = GridArea.Create("123");
    private Period _period = new(SystemClock.Instance.GetCurrentInstant(), SystemClock.Instance.GetCurrentInstant());
    private Resolution _resolution = Resolution.Hourly;

    public static AggregationResultBuilder Result()
    {
        return new AggregationResultBuilder();
    }

    public AggregationResult Build()
    {
        return new AggregationResult(
            Guid.NewGuid(),
            _points,
            _gridArea,
            _meteringPointType,
            _measureUnit,
            _resolution,
            _period,
            _settlementType,
            _aggregatedForActor);
    }

    public AggregationResultBuilder WithGridArea(string gridAreaCode)
    {
        _gridArea = GridArea.Create(gridAreaCode);
        return this;
    }

    public AggregationResultBuilder WithPeriod(Instant startOfPeriod, Instant endOfPeriod)
    {
        _period = new Period(startOfPeriod, endOfPeriod);
        return this;
    }

    public AggregationResultBuilder WithResolution(string resolution)
    {
        _resolution = Resolution.From(resolution);
        return this;
    }
}
