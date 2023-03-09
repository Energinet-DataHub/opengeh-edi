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
using Domain.OutgoingMessages;
using Domain.Transactions.Aggregations;
using NodaTime;
using Period = Domain.Transactions.Aggregations.Period;

namespace Tests.Factories;

public class AggregationResultBuilder
{
    private MeteringPointType _meteringPointType = MeteringPointType.Consumption;

    public AggregationResultBuilder WithMeteringPointType(MeteringPointType meteringPointType)
    {
        _meteringPointType = meteringPointType;
        return this;
    }

    #pragma warning disable
    public Aggregation Build()
    {
        return new Aggregation(
            new List<Point>(),
            "870",
            MeteringPointType.Consumption.Name,
            MeasurementUnit.Kwh.Name,
            Resolution.QuarterHourly.Name,
            new Period(
                SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(5)),
                SystemClock.Instance.GetCurrentInstant()),
            SettlementType.NonProfiled.Name,
            ProcessType.BalanceFixing.Name,
            null);
    }
}
