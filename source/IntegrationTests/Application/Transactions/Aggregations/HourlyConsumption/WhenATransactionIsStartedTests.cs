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
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Domain.Transactions;
using Domain.Transactions.Aggregations;
using IntegrationTests.Assertions;
using IntegrationTests.Fixtures;
using IntegrationTests.TestDoubles;
using Xunit;
using Period = Application.Transactions.Aggregations.Period;
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

        var startTransaction = new StartTransaction(
            SampleData.ResultId,
            SampleData.GridAreaCode,
            SampleData.EnergySupplierNumber.Value,
            new Period(SampleData.StartOfPeriod, SampleData.EndOfPeriod));
        await InvokeCommandAsync(startTransaction).ConfigureAwait(false);

        using var connection =
            await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync().ConfigureAwait(false);
        var transaction = await connection
            .QueryFirstOrDefaultAsync("SELECT * FROM dbo.AggregatedTimeSeriesTransactions");
        Assert.NotNull(transaction);
        Assert.Equal(SampleData.EnergySupplierNumber.Value, transaction.ReceivingActor);
        Assert.Equal(MarketRole.EnergySupplier.Name, transaction.ReceivingActorRole);
        Assert.Equal(ProcessType.BalanceFixing.Name, transaction.ProcessType);
    }

    [Fact]
    public async Task Aggregation_result_is_sent_to_energy_suppliers()
    {
        MakeAggregationResultAvailableFor(SampleData.EnergySupplierNumber);

        var startTransaction = new StartTransaction(
            SampleData.ResultId,
            SampleData.GridAreaCode,
            SampleData.EnergySupplierNumber.Value,
            new Period(SampleData.StartOfPeriod, SampleData.EndOfPeriod));
        await InvokeCommandAsync(startTransaction).ConfigureAwait(false);

        var outgoingMessage = await AssertOutgoingMessage.OutgoingMessageAsync(
            MessageType.NotifyAggregatedMeasureData.Name,
            ProcessType.BalanceFixing.Code,
            MarketRole.EnergySupplier,
            GetService<IDatabaseConnectionFactory>()).ConfigureAwait(false);
        outgoingMessage
            .HasReceiverId(SampleData.EnergySupplierNumber.Value)
            .HasReceiverRole(MarketRole.EnergySupplier.Name)
            .HasSenderId(DataHubDetails.IdentificationNumber.Value)
            .HasSenderRole(MarketRole.MeteringDataAdministrator.Name)
            .HasMessageRecordValue<TimeSeries>(timeSeries => timeSeries.Period.Start, SampleData.StartOfPeriod)
            .HasMessageRecordValue<TimeSeries>(timeSeries => timeSeries.Period.End, SampleData.EndOfPeriod)
            .HasMessageRecordValue<TimeSeries>(timeSeries => timeSeries.GridAreaCode, SampleData.GridAreaCode)
            .HasMessageRecordValue<TimeSeries>(timeSeries => timeSeries.MeteringPointType, MeteringPointType.Consumption.Name);
    }

    [Fact]
    public async Task Consumption_per_energy_supplier_result_is_sent_to_the_balance_responsible()
    {
        await InvokeCommandAsync(new SendAggregationResult(
            SampleData.BalanceResponsibleNumber,
            MarketRole.BalanceResponsible,
            ProcessType.BalanceFixing,
            CreateAggregatedConsumptionResult())).ConfigureAwait(false);

        var outgoingMessage = await AssertOutgoingMessage.OutgoingMessageAsync(
            MessageType.NotifyAggregatedMeasureData.Name,
            ProcessType.BalanceFixing.Code,
            MarketRole.BalanceResponsible,
            GetService<IDatabaseConnectionFactory>()).ConfigureAwait(false);
        outgoingMessage.HasReceiverId(SampleData.BalanceResponsibleNumber.Value);
    }

    private static AggregationResult CreateAggregatedConsumptionResult()
    {
        return AggregationResult.Consumption(
            SampleData.ResultId,
            GridArea.Create(SampleData.GridAreaCode),
            SettlementType.NonProfiled,
            MeasurementUnit.From(SampleData.MeasureUnitType),
            Resolution.From(SampleData.Resolution),
            new Domain.Transactions.Aggregations.Period(SampleData.StartOfPeriod, SampleData.EndOfPeriod),
            new List<Point>()
            {
                new(
                    1,
                    1.1m,
                    "A02",
                    "2022-10-31T21:15:00.000Z"),
            });
    }

    private void MakeAggregationResultAvailableFor(ActorNumber energySupplierNumber)
    {
        var result = AggregationResult.Consumption(
            SampleData.ResultId,
            GridArea.Create(SampleData.GridAreaCode),
            SettlementType.NonProfiled,
            MeasurementUnit.From(SampleData.MeasureUnitType),
            Resolution.From(SampleData.Resolution),
            new Domain.Transactions.Aggregations.Period(SampleData.StartOfPeriod, SampleData.EndOfPeriod),
            new List<Point>()
            {
                new(
                    1,
                    1.1m,
                    "A02",
                    "2022-10-31T21:15:00.000Z"),
            });

        var results = GetService<IAggregationResults>() as AggregationResultsStub;

        results?.Add(result, energySupplierNumber);
    }
}
