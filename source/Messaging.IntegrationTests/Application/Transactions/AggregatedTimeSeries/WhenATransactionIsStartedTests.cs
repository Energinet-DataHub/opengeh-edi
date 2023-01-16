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
using Messaging.Infrastructure.Configuration.InternalCommands;
using Messaging.Infrastructure.Transactions.AggregatedTimeSeries;
using Messaging.IntegrationTests.Assertions;
using Messaging.IntegrationTests.Fixtures;
using Xunit;
using Point = Messaging.Infrastructure.Transactions.AggregatedTimeSeries.Point;

namespace Messaging.IntegrationTests.Application.Transactions.AggregatedTimeSeries;

public class WhenATransactionIsStartedTests : TestBase
{
    public WhenATransactionIsStartedTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    [Fact]
    public async Task Result_Is_Retrieved()
    {
        await StartTransaction().ConfigureAwait(false);

        AssertQueuedCommand.QueuedCommand<RetrieveAggregationResult>(GetService<IDatabaseConnectionFactory>(), GetService<InternalCommandMapper>());
    }

    [Fact]
    public async Task Result_Is_Forwarded_To_The_Receiver()
    {
        AddFakeResult();
        await StartTransaction().ConfigureAwait(false);
        var transactionId = await GetTransactionIdAsync().ConfigureAwait(false);

        await InvokeCommandAsync(new RetrieveAggregationResult(SampleData.ResultId, transactionId)).ConfigureAwait(false);

        var message = await AssertOutgoingMessage.OutgoingMessageAsync(
                 transactionId.ToString(),
                 MessageType.NotifyAggregatedMeasureData.Name,
                 ProcessType.BalanceFixing.Code,
                 MarketRole.GridOperator,
                 GetService<IDatabaseConnectionFactory>()).ConfigureAwait(false);
        message.HasReceiverId(SampleData.GridOperatorNumber)
                 .HasReceiverRole(MarketRole.GridOperator.Name)
                 .HasSenderRole(MarketRole.MeteringDataAdministrator.Name)
                 .HasSenderId(DataHubDetails.IdentificationNumber.Value)
                 .HasMessageRecordValue<TimeSeries>(x => x.TransactionId, transactionId.ToString())
                 .HasMessageRecordValue<TimeSeries>(x => x.GridAreaCode, SampleData.GridAreaCode)
                 .HasMessageRecordValue<TimeSeries>(x => x.Resolution, SampleData.Resolution)
                 .HasMessageRecordValue<TimeSeries>(x => x.MeasureUnitType, SampleData.MeasureUnitType)
                 .HasMessageRecordValue<TimeSeries>(x => x.MeteringPointType, SampleData.MeteringPointType)
                 .HasMessageRecordValue<TimeSeries>(x => x.Point[0].Position, 1)
                 .HasMessageRecordValue<TimeSeries, decimal?>(x => x.Point[0].Quantity, 1.1m)
                 .HasMessageRecordValue<TimeSeries>(x => x.Point[0].Quality!, "A02");
    }

    private void AddFakeResult()
    {
        var results = GetService<IAggregatedTimeSeriesResults>() as FakeAggregatedTimeSeriesResults;
        var dto = new AggregatedTimeSeriesResultDto(
            SampleData.GridAreaCode,
            SampleData.MeteringPointType,
            SampleData.MeasureUnitType,
            SampleData.Resolution,
            new List<Point>()
            {
                new(
                    1,
                    "1.1",
                    "A02",
                    "2022-10-31T21:15:00.000Z"),
            });

        results?.Add(SampleData.ResultId, dto);
    }

    private async Task<Guid> GetTransactionIdAsync()
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync().ConfigureAwait(false);
        return await connection
            .QueryFirstAsync<Guid>("SELECT TOP(1) Id FROM b2b.AggregatedTimeSeriesTransactions").ConfigureAwait(false);
    }

    private async Task StartTransaction()
    {
        await InvokeCommandAsync(new StartTransaction(SampleData.GridAreaCode, SampleData.ResultId));
    }
}
