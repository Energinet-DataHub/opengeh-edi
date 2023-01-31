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
using Application.Configuration.DataAccess;
using Application.Transactions.Aggregations;
using Dapper;
using Domain.Actors;
using Domain.OutgoingMessages;
using Domain.SeedWork;
using Energinet.DataHub.Wholesale.Contracts.Events;
using Google.Protobuf.WellKnownTypes;
using Infrastructure.Configuration.InternalCommands;
using IntegrationTests.Assertions;
using IntegrationTests.Fixtures;
using Xunit;

namespace IntegrationTests.Application.Transactions.Aggregations;

public class WhenAggregationOfTotalProductionIsCompletedTests : TestBase
{
    public WhenAggregationOfTotalProductionIsCompletedTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    [Fact]
    public async Task A_transaction_is_started()
    {
        await HavingReceivedIntegrationEventAsync(SampleData.NameOfBalanceFixingCompletedIntegrationEvent, BalanceFixingCompletedIntegrationEvent())
            .ConfigureAwait(false);

        await ForwardAggregationResultTransactionHasBeenStarted().ConfigureAwait(false);
    }

    [Fact]
    public async Task Aggregation_result_retrieval_is_scheduled()
    {
        await StartTransaction().ConfigureAwait(false);

        AssertQueuedCommand.QueuedCommand<RetrieveAggregationResult>(GetService<IDatabaseConnectionFactory>(), GetService<InternalCommandMapper>());
    }

    private static ProcessCompleted BalanceFixingCompletedIntegrationEvent()
    {
        return new ProcessCompleted()
        {
            GridAreaCode = SampleData.GridAreaCode,
            BatchId = SampleData.ResultId.ToString(),
            PeriodStartUtc = Timestamp.FromDateTime(SampleData.StartOfPeriod.ToDateTimeUtc()),
            PeriodEndUtc = Timestamp.FromDateTime(SampleData.EndOfPeriod.ToDateTimeUtc()),
        };
    }

    private async Task ForwardAggregationResultTransactionHasBeenStarted()
    {
        var gridAreaLookup = GetService<IGridAreaLookup>();
        var gridOperatorNumber =
            await gridAreaLookup.GetGridOperatorForAsync(SampleData.GridAreaCode).ConfigureAwait(false);

        using var connection =
            await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync().ConfigureAwait(false);
        var transaction = await connection
            .QueryFirstOrDefaultAsync("SELECT * FROM dbo.AggregatedTimeSeriesTransactions");
        Assert.NotNull(transaction);
        Assert.Equal(MarketRole.MeteredDataResponsible, EnumerationType.FromName<MarketRole>(transaction.ReceivingActorRole));
        Assert.Equal(ProcessType.BalanceFixing, EnumerationType.FromName<ProcessType>(transaction.ProcessType));
        Assert.Equal(gridOperatorNumber.Value, transaction.ReceivingActor);
        Assert.Equal(SampleData.StartOfPeriod.ToDateTimeUtc(), transaction.PeriodStart);
        Assert.Equal(SampleData.EndOfPeriod.ToDateTimeUtc(), transaction.PeriodEnd);
    }

    private async Task StartTransaction()
    {
        await InvokeCommandAsync(new StartTransaction(SampleData.GridAreaCode, SampleData.ResultId, SampleData.StartOfPeriod, SampleData.EndOfPeriod));
    }
}
