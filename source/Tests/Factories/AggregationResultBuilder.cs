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
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Common.Actors;
using Energinet.DataHub.EDI.Domain.GridAreaOwners;
using Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations;
using NodaTime;
using Period = Energinet.DataHub.EDI.Common.Period;

namespace Energinet.DataHub.EDI.Tests.Factories;

public class AggregationResultBuilder
{
    private MeteringPointType _meteringPointType = MeteringPointType.Consumption;
    private GridAreaOwner _gridAreaOwner = new("870", Instant.FromDateTimeUtc(DateTime.UtcNow), ActorNumber.Create("1234567890123"), 1);
    private SettlementType? _settlementType;
    private ActorGrouping _actorGrouping = new ActorGrouping(null, null);

    public Aggregation Build()
    {
        return new Aggregation(
            new List<Point>(),
            _meteringPointType.Name,
            MeasurementUnit.Kwh.Name,
            Resolution.QuarterHourly.Name,
            new Period(
                SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(5)),
                SystemClock.Instance.GetCurrentInstant()),
            _settlementType?.Name,
            BusinessReason.BalanceFixing.Name,
            _actorGrouping,
            new GridAreaDetails(_gridAreaOwner.GridAreaCode, _gridAreaOwner.GridAreaOwnerActorNumber.Value));
    }

    public AggregationResultBuilder WithGridAreaDetails(GridAreaOwner gridAreaOwner)
    {
        _gridAreaOwner = gridAreaOwner;
        return this;
    }

    public AggregationResultBuilder ForProduction()
    {
        _meteringPointType = MeteringPointType.Production;
        return this;
    }

    public AggregationResultBuilder ForConsumption(SettlementType? settlementType)
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
