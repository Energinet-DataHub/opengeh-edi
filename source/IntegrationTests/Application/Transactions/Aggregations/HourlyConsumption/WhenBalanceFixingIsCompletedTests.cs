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
using Application.Transactions.Aggregations.HourlyConsumption;
using Domain.Actors;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Domain.Transactions.Aggregations;
using Energinet.DataHub.Wholesale.Contracts.Events;
using Google.Protobuf.WellKnownTypes;
using Infrastructure.Configuration.InternalCommands;
using IntegrationTests.Assertions;
using IntegrationTests.Fixtures;
using IntegrationTests.TestDoubles;
using Xunit;
using StartTransaction = Application.Transactions.Aggregations.HourlyConsumption.StartTransaction;

namespace IntegrationTests.Application.Transactions.Aggregations.HourlyConsumption;

public class WhenBalanceFixingIsCompletedTests : TestBase
{
    public WhenBalanceFixingIsCompletedTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    [Fact]
    public async Task Aggregation_result_retrieval_is_scheduled()
    {
        await WhenBalanceFixingIsCompleted();

        CheckHasEnqueuedCommand<PrepareTransactions>();
    }

    [Fact]
    public async Task Aggregation_result_forwarding_transactions_are_prepared_and_scheduled()
    {
        MakeAggregationResultAvailableFor(SampleData.EnergySupplierNumber);
        MakeAggregationResultAvailableFor(SampleData.EnergySupplierNumber2);

        await WhenBalanceFixingIsCompleted();
        await HavingProcessedInternalTasksAsync().ConfigureAwait(false);

        CheckHasEnqueuedCommand<StartTransaction>()
            .CountIs(2);
    }

    private async Task WhenBalanceFixingIsCompleted()
    {
        var integrationEvent = new ProcessCompleted()
        {
            GridAreaCode = SampleData.GridAreaCode,
            BatchId = SampleData.ResultId.ToString(),
            PeriodStartUtc = Timestamp.FromDateTime(SampleData.StartOfPeriod.ToDateTimeUtc()),
            PeriodEndUtc = Timestamp.FromDateTime(SampleData.EndOfPeriod.ToDateTimeUtc()),
        };
        await HavingReceivedIntegrationEventAsync("BalanceFixingCompleted", integrationEvent).ConfigureAwait(false);
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
            MeteringPointType.Production,
            SampleData.MeasureUnitType,
            SampleData.Resolution,
            new Domain.Transactions.Aggregations.Period(SampleData.StartOfPeriod, SampleData.EndOfPeriod));

        var results = GetService<IAggregationResults>() as AggregationResultsStub;

        results?.Add(result, energySupplierNumber);
    }

    private AssertQueuedCommand CheckHasEnqueuedCommand<TCommandType>()
    {
        return AssertQueuedCommand.QueuedCommand<TCommandType>(
            GetService<IDatabaseConnectionFactory>(),
            GetService<InternalCommandMapper>());
    }
}
