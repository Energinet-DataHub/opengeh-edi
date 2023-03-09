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
using Domain.Transactions.Aggregations;
using Tests.Factories;
using Xunit;

namespace Tests.Domain.Transactions.Aggregations;

public class TransactionFactoryTests
{
    [Fact]
    public void Create_transaction_for_grid_operator_when_result_is_total_production()
    {
        var gridOperatorNumber = ActorNumber.Create("1234567890123");
        var factory = new TransactionFactory(gridOperatorNumber);
        var aggregationResult = new AggregationResultBuilder();
        aggregationResult
            .WithMeteringPointType(MeteringPointType.Production);
        var aggregation = aggregationResult.Build();
        var transaction = factory.CreateFrom(aggregation);

        var message = transaction.CreateMessage(aggregation);

        Assert.Equal(MarketRole.MeteredDataResponsible, message.ReceiverRole);
        Assert.Equal(gridOperatorNumber, message.ReceiverId);
    }
}
