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
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Domain.Transactions;
using Domain.Transactions.Aggregations;
using NodaTime;
using Xunit;
using Period = Domain.Transactions.Aggregations.Period;

namespace Tests.Domain.Transactions.Aggregations;

public class AggregationResultTests
{
    [Fact]
    public void Create_a_result_for_consumption()
    {
        var result = AggregationResult.Consumption(
            Guid.NewGuid(),
            GridArea.Create("543"),
            SettlementType.NonProfiled,
            "KWH",
            "PTH1",
            new Period(SystemClock.Instance.GetCurrentInstant(), SystemClock.Instance.GetCurrentInstant()),
            new List<Point>());

        Assert.Equal(MeteringPointType.Consumption, result.MeteringPointType);
        Assert.Equal(SettlementType.NonProfiled, result.SettlementType);
    }

    [Fact]
    public void Create_a_result_for_production()
    {
        var result = AggregationResult.Production(
            Guid.NewGuid(),
            GridArea.Create("543"),
            "KWH",
            "PTH1",
            new Period(SystemClock.Instance.GetCurrentInstant(), SystemClock.Instance.GetCurrentInstant()),
            new List<Point>());

        Assert.Equal(MeteringPointType.Production, result.MeteringPointType);
        Assert.Null(result.SettlementType);
    }
}
