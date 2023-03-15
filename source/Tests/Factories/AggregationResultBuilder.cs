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
using Domain.Actors;
using Domain.OutgoingMessages;
using Domain.Transactions;
using Domain.Transactions.Aggregations;
using NodaTime;
using Period = Domain.Transactions.Aggregations.Period;

namespace Tests.Factories;

public class AggregationResultBuilder
{
    private MeteringPointType _meteringPointType = MeteringPointType.Consumption;
    private GridArea _gridArea = GridArea.Create("870");
    private ActorNumber _gridOperator = ActorNumber.Create("1234567890123");
    private SettlementType? _settlementType;
    private ActorGrouping _actorGrouping = new ActorGrouping(null, null);

    public Aggregation Build()
    {
        return new Aggregation(
            new List<Point>(),
            "870",
            _meteringPointType.Name,
            MeasurementUnit.Kwh.Name,
            Resolution.QuarterHourly.Name,
            new Period(
                SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(5)),
                SystemClock.Instance.GetCurrentInstant()),
            SettlementType.NonProfiled.Name,
            ProcessType.BalanceFixing.Name,
            _actorGrouping,
            new GridAreaDetails(_gridArea.Code, _gridOperator.Value));
    }

    public AggregationResultBuilder WithGridAreaDetails(GridArea gridArea, ActorNumber gridOperator)
    {
        _gridArea = gridArea;
        _gridOperator = gridOperator;
        return this;
    }

    public AggregationResultBuilder ForProduction()
    {
        _meteringPointType = MeteringPointType.Production;
        _settlementType = null;
        return this;
    }

    public AggregationResultBuilder ForConsumption(SettlementType settlementType)
    {
        _meteringPointType = MeteringPointType.Consumption;
        _settlementType = settlementType;
        return this;
    }

    public AggregationResultBuilder WithGrouping(ActorNumber? energySupplierNumber, ActorNumber? balanceResponsibleNumber)
    {
        _actorGrouping = new ActorGrouping(energySupplierNumber?.Value, balanceResponsibleNumber?.Value);
        return this;
    }
}
