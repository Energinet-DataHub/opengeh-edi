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
using Domain.OutgoingMessages;
using Energinet.DataHub.Wholesale.Contracts.Events;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Infrastructure.Configuration.InternalCommands;
using IntegrationTests.Assertions;
using IntegrationTests.Fixtures;
using Xunit;

namespace IntegrationTests.Application.Transactions.Aggregations;

public class WhenAnAggregationProcessHasCompletedTests : TestBase
{
    public WhenAnAggregationProcessHasCompletedTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    public static IEnumerable<object[]> AggregationProcessCompletedEvents()
    {
        return new[] { new object[] { SampleData.NameOfBalanceFixingCompletedIntegrationEvent, BalanceFixingCompletedIntegrationEvent(), ProcessType.BalanceFixing }, };
    }

    [Theory]
    [MemberData(nameof(AggregationProcessCompletedEvents))]
    public async Task Aggregation_results_are_retrieved(string nameOfReceivedEvent, IMessage receivedEvent, ProcessType aggregationProcessType)
    {
        await HavingReceivedIntegrationEventAsync(nameOfReceivedEvent, receivedEvent)
            .ConfigureAwait(false);

        EnqueuedCommand<RetrieveAggregationResults>()
            .HasProperty<RetrieveAggregationResults>(command => command.AggregationProcess, aggregationProcessType);
    }

    private static IMessage BalanceFixingCompletedIntegrationEvent()
    {
        return new ProcessCompleted()
        {
            GridAreaCode = SampleData.GridAreaCode,
            BatchId = SampleData.ResultId.ToString(),
            PeriodStartUtc = Timestamp.FromDateTime(SampleData.StartOfPeriod.ToDateTimeUtc()),
            PeriodEndUtc = Timestamp.FromDateTime(SampleData.EndOfPeriod.ToDateTimeUtc()),
        };
    }

    private AssertQueuedCommand EnqueuedCommand<TCommandType>()
    {
        return AssertQueuedCommand.QueuedCommand<TCommandType>(
            GetService<IDatabaseConnectionFactory>(),
            GetService<InternalCommandMapper>());
    }
}
