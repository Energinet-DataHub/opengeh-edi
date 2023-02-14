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
using Application.Configuration.DataAccess;
using Application.Transactions.Aggregations;
using Domain.Actors;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using IntegrationTests.Assertions;
using IntegrationTests.Fixtures;
using IntegrationTests.TestDoubles;
using Xunit;

namespace IntegrationTests.Application.Transactions.Aggregations;

#pragma warning disable CA1062 // To avoid null guards in parameterized tests
public class WhenResultsAreRetrievedTests : TestBase
{
    private readonly AggregationResultsStub _aggregationResults;

    public WhenResultsAreRetrievedTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _aggregationResults = (AggregationResultsStub)GetService<IAggregationResults>();
    }

    public static IEnumerable<object[]> AggregationProcessTypes()
    {
        return new[] { new object[] { ProcessType.BalanceFixing }, };
    }

    [Theory]
    [MemberData(nameof(AggregationProcessTypes))]
    public async Task Consumption_per_energy_supplier_result_is_sent_to_the_balance_responsible(ProcessType completedAggregationType)
    {
        _aggregationResults.HasNonProfiledConsumptionFor(
            SampleData.BalanceResponsibleNumber,
            new List<ActorNumber>()
            {
                SampleData.EnergySupplierNumber,
            }.AsReadOnly());

        await InvokeCommandAsync(new RetrieveAggregationResults(
            SampleData.ResultId,
            completedAggregationType.Name,
            SampleData.GridAreaCode,
            new Period(SampleData.StartOfPeriod, SampleData.EndOfPeriod))).ConfigureAwait(false);

        await HavingProcessedInternalTasksAsync().ConfigureAwait(false);

        var outgoingMessage = await AssertOutgoingMessage.OutgoingMessageAsync(
            MessageType.NotifyAggregatedMeasureData.Name,
            completedAggregationType.Code,
            MarketRole.BalanceResponsible,
            GetService<IDatabaseConnectionFactory>()).ConfigureAwait(false);
        outgoingMessage
            .HasReceiverId(SampleData.BalanceResponsibleNumber.Value)
            .HasReceiverRole(MarketRole.BalanceResponsible.Name)
            .HasSenderId(DataHubDetails.IdentificationNumber.Value)
            .HasSenderRole(MarketRole.MeteringDataAdministrator.Name)
            .HasMessageRecordValue<TimeSeries>(
                series => series.BalanceResponsibleNumber!,
                SampleData.BalanceResponsibleNumber.Value)
            .HasMessageRecordValue<TimeSeries>(
                series => series.EnergySupplierNumber!,
                SampleData.EnergySupplierNumber.Value);
    }
}
