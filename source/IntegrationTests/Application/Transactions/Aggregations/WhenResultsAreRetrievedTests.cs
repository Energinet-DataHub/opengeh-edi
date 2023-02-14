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
using Domain.Transactions;
using IntegrationTests.Assertions;
using IntegrationTests.Factories;
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
    public async Task Total_production_result_is_sent_to_the_grid_operator(ProcessType completedAggregationType)
    {
        _aggregationResults.HasResult(AggregationResultBuilder
            .Result()
            .WithGridArea(SampleData.GridAreaCode)
            .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod)
            .WithResolution(SampleData.Resolution)
            .Build());

        await RetrieveResults(completedAggregationType).ConfigureAwait(false);
        await HavingProcessedInternalTasksAsync().ConfigureAwait(false);

        var message = await OutgoingMessageAsync(
            MessageType.NotifyAggregatedMeasureData, MarketRole.MeteredDataResponsible, completedAggregationType);
        message.HasReceiverId(SampleData.GridOperatorNumber)
            .HasReceiverRole(MarketRole.MeteredDataResponsible.Name)
            .HasSenderRole(MarketRole.MeteringDataAdministrator.Name)
            .HasSenderId(DataHubDetails.IdentificationNumber.Value)
            .HasMessageRecordValue<TimeSeries>(x => x.GridAreaCode, SampleData.GridAreaCode)
            .HasMessageRecordValue<TimeSeries>(x => x.Resolution, SampleData.Resolution)
            .HasMessageRecordValue<TimeSeries>(x => x.MeasureUnitType, MeasurementUnit.Kwh.Code)
            .HasMessageRecordValue<TimeSeries>(x => x.MeteringPointType, MeteringPointType.Production.Name)
            .HasMessageRecordValue<TimeSeries>(x => x.Period.Start, SampleData.StartOfPeriod)
            .HasMessageRecordValue<TimeSeries>(x => x.Period.End, SampleData.EndOfPeriod)
            .HasMessageRecordValue<TimeSeries>(x => x.Point[0].Position, 1)
            .HasMessageRecordValue<TimeSeries, decimal?>(x => x.Point[0].Quantity, 1.1m)
            .HasMessageRecordValue<TimeSeries>(x => x.Point[0].Quality!, "A02");
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

        await RetrieveResults(completedAggregationType).ConfigureAwait(false);
        await HavingProcessedInternalTasksAsync().ConfigureAwait(false);

        var outgoingMessage = await OutgoingMessageAsync(
            MessageType.NotifyAggregatedMeasureData,
            MarketRole.BalanceResponsible,
            completedAggregationType);
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

    private async Task<AssertOutgoingMessage> OutgoingMessageAsync(MessageType messageType, MarketRole roleOfReceiver, ProcessType completedAggregationType)
    {
        return await AssertOutgoingMessage.OutgoingMessageAsync(
            messageType.Name,
            completedAggregationType.Code,
            roleOfReceiver,
            GetService<IDatabaseConnectionFactory>()).ConfigureAwait(false);
    }

    private async Task RetrieveResults(ProcessType completedAggregationType)
    {
        await InvokeCommandAsync(new RetrieveAggregationResults(
            SampleData.ResultId,
            completedAggregationType.Name,
            SampleData.GridAreaCode,
            new Period(SampleData.StartOfPeriod, SampleData.EndOfPeriod))).ConfigureAwait(false);
    }
}
