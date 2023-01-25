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

using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.Transactions.Aggregations;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.SeedWork;
using Messaging.Infrastructure.Configuration.InternalCommands;
using Messaging.IntegrationTests.Assertions;
using Messaging.IntegrationTests.Fixtures;
using NodaTime;
using Xunit;

namespace Messaging.IntegrationTests.Application.Transactions.Aggregations;

public class WhenAggregationOfTotalProductionIsCompletedTests : TestBase
{
    public WhenAggregationOfTotalProductionIsCompletedTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    [Fact]
    public async Task A_transaction_is_started()
    {
        var gridAreaLookup = GetService<IGridAreaLookup>();
        var gridOperatorNumber = await gridAreaLookup.GetGridOperatorForAsync(SampleData.GridAreaCode).ConfigureAwait(false);
        await StartTransaction().ConfigureAwait(false);

        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync().ConfigureAwait(false);
        var transaction = await connection
            .QueryFirstOrDefaultAsync("SELECT * FROM b2b.AggregatedTimeSeriesTransactions");
        Assert.NotNull(transaction);
        Assert.Equal(MarketRole.GridOperator, EnumerationType.FromName<MarketRole>(transaction.ReceivingActorRole));
        Assert.Equal(ProcessType.BalanceFixing, EnumerationType.FromName<ProcessType>(transaction.ProcessType));
        Assert.Equal(gridOperatorNumber.Value, transaction.ReceivingActor);
    }

    [Fact]
    public async Task Aggregation_result_retrieval_is_scheduled()
    {
        await StartTransaction().ConfigureAwait(false);

        AssertQueuedCommand.QueuedCommand<RetrieveAggregationResult>(GetService<IDatabaseConnectionFactory>(), GetService<InternalCommandMapper>());
    }

    private async Task StartTransaction()
    {
        await InvokeCommandAsync(new StartTransaction(SampleData.GridAreaCode, SampleData.ResultId, SampleData.StartOfPeriod, SampleData.EndOfPeriod));
    }
}
