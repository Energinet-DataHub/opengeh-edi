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
using Domain.Transactions;
using Domain.Transactions.Aggregations;
using Tests.Factories;
using Xunit;

namespace Tests.Domain.Transactions.Aggregations;

public class AggregationResultForwardingTests
{
    private readonly AggregationResultBuilder _aggregationResult;

    public AggregationResultForwardingTests()
    {
        _aggregationResult = new AggregationResultBuilder();
    }

    [Fact]
    public void Create_message_for_grid_operator_when_result_is_total_production()
    {
        var result = _aggregationResult
            .ForProduction()
            .WithGridAreaDetails(GridArea.Create("870"), ActorNumber.Create("1234567890123"))
            .Build();

        var message = CreateMessage(result);

        Assert.Equal(MarketRole.MeteredDataResponsible, message.ReceiverRole);
        Assert.Equal(result.GridAreaDetails?.OperatorNumber, message.ReceiverId.Value);
    }

    [Fact]
    public void Create_message_for_grid_operator_when_result_is_total_non_profiled_consumption()
    {
        var result = _aggregationResult
            .ForConsumption(SettlementType.NonProfiled)
            .Build();

        var message = CreateMessage(result);

        Assert.Equal(MarketRole.MeteredDataResponsible, message.ReceiverRole);
        Assert.Equal(result.GridAreaDetails?.OperatorNumber, message.ReceiverId.Value);
        Assert.Equal(SettlementType.NonProfiled.Name, message.Series.SettlementType);
    }

    [Fact]
    public void Create_message_for_energy_supplier_when_result_is_non_profiled_consumption()
    {
        var result = _aggregationResult
            .ForConsumption(SettlementType.NonProfiled)
            .WithGrouping(ActorNumber.Create("1234567890123"), null)
            .Build();

        var message = CreateMessage(result);

        Assert.Equal(MarketRole.EnergySupplier, message.ReceiverRole);
        Assert.Equal(result.ActorGrouping?.EnergySupplierNumber, message.ReceiverId.Value);
        Assert.Equal(SettlementType.NonProfiled.Name, message.Series.SettlementType);
    }

    [Fact]
    public void Create_message_for_balance_responsible_when_result_is_non_profiled_consumption()
    {
        var result = _aggregationResult
            .ForConsumption(SettlementType.NonProfiled)
            .WithGrouping(ActorNumber.Create("1234567890123"), ActorNumber.Create("1234567890124"))
            .Build();

        var message = CreateMessage(result);

        Assert.Equal(MarketRole.BalanceResponsible, message.ReceiverRole);
        Assert.Equal(result.ActorGrouping?.BalanceResponsibleNumber, message.ReceiverId.Value);
        Assert.Equal(SettlementType.NonProfiled.Name, message.Series.SettlementType);
    }

    private static AggregationResultMessage CreateMessage(Aggregation result)
    {
        var transaction = CreateTransaction();
        return transaction.CreateMessage(result);
    }

    private static AggregationResultForwarding CreateTransaction()
    {
        var transaction = new AggregationResultForwarding(
            TransactionId.New(),
            ActorNumber.Create("1234567890123"),
            MarketRole.EnergySupplier,
            ProcessType.MoveIn);
        return transaction;
    }
}
