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
using Domain.Actors;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Domain.Transactions;
using Domain.Transactions.Aggregations;
using Tests.Factories;
using Xunit;
using Point = Domain.OutgoingMessages.NotifyAggregatedMeasureData.Point;

namespace Tests.Domain.OutgoingMessages;

public class AggregationResultMessageTests
{
    [Fact]
    public void Receiver_id_must_be_included_in_series_if_receiver_is_balance_responsible()
    {
        var receiverNumber = ActorNumber.Create("1234567890123");

        var message = CreateMessageFor(receiverNumber, CreateResult());

        Assert.Equal(receiverNumber.Value, message.Series.BalanceResponsibleNumber);
    }

    [Fact]
    public void Energy_supplier_number_must_be_include_in_series_if_receiver_is_balance_responsible()
    {
        var aggregationResult = CreateResult(ActorNumber.Create("1234567890124"));

        var message = CreateMessageFor(ActorNumber.Create("1234567890123"), aggregationResult);

        Assert.Equal(aggregationResult.AggregatedForActor?.Value, message.Series.EnergySupplierNumber);
    }

    private static AggregationResult CreateResult(ActorNumber? aggregatedForActorNumber = null)
    {
        return AggregationResult.Consumption(
            Guid.NewGuid(),
            GridArea.Create("543"),
            SettlementType.NonProfiled,
            MeasurementUnit.Kwh,
            Resolution.Hourly,
            new Period(EffectiveDateFactory.InstantAsOfToday(), EffectiveDateFactory.InstantAsOfToday()),
            new List<Point>(),
            aggregatedForActorNumber ?? null);
    }

    private static AggregationResultMessage CreateMessageFor(ActorNumber receiverNumber, AggregationResult result)
    {
        return AggregationResultMessage.Create(
            receiverNumber,
            MarketRole.BalanceResponsible,
            TransactionId.New(),
            ProcessType.BalanceFixing,
            result);
    }
}
