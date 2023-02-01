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
using Application.Transactions.Aggregations.HourlyConsumption;
using Domain.Actors;
using Domain.OutgoingMessages;
using Energinet.DataHub.Wholesale.Contracts.Events;
using Google.Protobuf.WellKnownTypes;
using Infrastructure.Configuration.InternalCommands;
using IntegrationTests.Assertions;
using IntegrationTests.Fixtures;
using Xunit;

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
        var integrationEvent = BalanceFixingCompletedIntegrationEvent();
        await HavingReceivedIntegrationEventAsync("BalanceFixingCompleted", integrationEvent).ConfigureAwait(false);

        AssertQueuedCommand.QueuedCommand<FetchResultOfHourlyConsumption>(
            GetService<IDatabaseConnectionFactory>(),
            GetService<InternalCommandMapper>());
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
}
