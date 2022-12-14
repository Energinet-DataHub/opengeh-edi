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
using System.Threading.Tasks;
using Dapper;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.Transactions.AggregatedTimeSeries;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Messaging.IntegrationTests.Assertions;
using Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace Messaging.IntegrationTests.Application.Transactions.AggregatedTimeSeries;

public class SendAggregatedTimeSeriesTests : TestBase
{
    public SendAggregatedTimeSeriesTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    [Fact]
    public async Task A_transaction_is_started()
    {
        await InvokeCommandAsync(CreateRequest()).ConfigureAwait(false);

        var exists = await GetService<IDbConnectionFactory>().GetOpenConnection()
            .ExecuteScalarAsync<bool>("SELECT COUNT(*) FROM b2b.AggregatedTimeSeriesTransactions");
        Assert.True(exists);
    }

    [Fact]
    public async Task Aggregated_time_series_result_is_send_to_the_grid_operator()
    {
        await InvokeCommandAsync(CreateRequest()).ConfigureAwait(false);

        AssertOutgoingMessage.OutgoingMessage(
            MessageType.AggregatedTimeSeries.Name,
            ProcessType.BalanceFixing.Code,
            MarketRole.GridOperator,
            GetService<IDbConnectionFactory>());
    }

    private static SendAggregatedTimeSeries CreateRequest()
    {
        var timeSeries = new TimeSeries(
            Guid.NewGuid(),
            "E18",
            "KWH",
            new Period("PT1H", new TimeInterval("2022-02-12T23:00Z", "2022-02-12T23:00Z"), new List<Point>()
            {
                new(1, 11, null),
            }));
        return new SendAggregatedTimeSeries(timeSeries, SampleData.GridOperatorNumber);
    }
}
