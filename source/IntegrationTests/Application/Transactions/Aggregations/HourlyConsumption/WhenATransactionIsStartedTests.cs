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
using System.Threading.Tasks;
using Application.Configuration.DataAccess;
using Application.Transactions.Aggregations;
using Dapper;
using Domain.Actors;
using Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Domain.Transactions.Aggregations;
using IntegrationTests.Fixtures;
using IntegrationTests.TestDoubles;
using Xunit;
using StartTransaction = Application.Transactions.Aggregations.HourlyConsumption.StartTransaction;

namespace IntegrationTests.Application.Transactions.Aggregations.HourlyConsumption;

public class WhenATransactionIsStartedTests : TestBase
{
    public WhenATransactionIsStartedTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    [Fact]
    public async Task Transaction_is_started_if_a_result_is_available()
    {
        MakeAggregationResultAvailableFor(SampleData.EnergySupplierNumber);

        var startTransaction = new StartTransaction(SampleData.ResultId, SampleData.GridAreaCode, SampleData.EnergySupplierNumber.Value);
        await InvokeCommandAsync(startTransaction).ConfigureAwait(false);

        using var connection =
            await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync().ConfigureAwait(false);
        var transaction = await connection
            .QueryFirstOrDefaultAsync("SELECT * FROM dbo.AggregatedTimeSeriesTransactions");
        Assert.NotNull(transaction);
    }

    private void MakeAggregationResultAvailableFor(ActorNumber energySupplierNumber)
    {
        var result = new AggregationResult(
            SampleData.ResultId,
            new List<Point>()
            {
                new(
                    1,
                    1.1m,
                    "A02",
                    "2022-10-31T21:15:00.000Z"),
            },
            SampleData.GridAreaCode,
            SampleData.MeteringPointType,
            SampleData.MeasureUnitType,
            SampleData.Resolution);

        var results = GetService<IAggregationResults>() as AggregationResultsStub;

        results?.Add(result, energySupplierNumber);
    }
}
