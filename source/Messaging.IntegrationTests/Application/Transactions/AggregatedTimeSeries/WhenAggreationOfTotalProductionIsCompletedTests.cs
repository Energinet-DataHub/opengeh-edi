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
using Dapper;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.Transactions.AggregatedTimeSeries;
using Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace Messaging.IntegrationTests.Application.Transactions.AggregatedTimeSeries;

public class WhenAggregationOfTotalProductionIsCompletedTests : TestBase
{
    public WhenAggregationOfTotalProductionIsCompletedTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    [Fact]
    public async Task A_transaction_is_started()
    {
        await InvokeCommandAsync(new StartTransaction(SampleData.GridAreaCode, SampleData.ResultId));

        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync().ConfigureAwait(false);
        var exists = await connection
            .ExecuteScalarAsync<bool>("SELECT COUNT(*) FROM b2b.AggregatedTimeSeriesTransactions");
        Assert.True(exists);
    }
}
