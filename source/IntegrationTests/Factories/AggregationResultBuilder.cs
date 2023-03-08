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
using Point = Domain.OutgoingMessages.NotifyAggregatedMeasureData.Point;

namespace IntegrationTests.Factories;

internal sealed class AggregationResultBuilder
{
    private static readonly IReadOnlyList<Point> _points = new List<Point>()
    {
        new(1, 1.1m, Quality.Missing.Name, "2022-10-31T21:15:00.000Z"),
    };

    private readonly MeasurementUnit _measureUnit = MeasurementUnit.Kwh;
    private readonly ActorNumber _aggregatedForActor = default!;
    private SettlementType? _settlementType = SettlementType.NonProfiled;
    private MeteringPointType _meteringPointType = MeteringPointType.Consumption;
    private GridArea _gridArea = GridArea.Create("123");
    private Period _period = new(SystemClock.Instance.GetCurrentInstant(), SystemClock.Instance.GetCurrentInstant());
    private Resolution _resolution = Resolution.Hourly;
    private ActorNumber _receivingActorNumber = default!;
    private MarketRole _receivingActorRole = default!;

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
            _aggregatedForActor,
            _receivingActorNumber,
            _receivingActorRole);
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

    public AggregationResultBuilder WithMeteringPointType(MeteringPointType meteringPointType)
    {
        _meteringPointType = meteringPointType;
        return this;
    }

    public AggregationResultBuilder WithReceivingActorNumber(ActorNumber receivingActorNumber)
    {
        _receivingActorNumber = receivingActorNumber;
        return this;
    }

    public AggregationResultBuilder WithReceivingActorRole(MarketRole receivingActorRole)
    {
        _receivingActorRole = receivingActorRole;
        return this;
    }

    public AggregationResultBuilder WithSettlementMethod(SettlementType settlementType)
    {
        _settlementType = settlementType;
        return this;
    }
}
