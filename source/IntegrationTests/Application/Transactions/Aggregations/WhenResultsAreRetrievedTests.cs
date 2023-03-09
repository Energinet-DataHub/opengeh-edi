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
using Domain.Transactions.Aggregations;
using Energinet.DataHub.Wholesale.Contracts.Events;
using Google.Protobuf.WellKnownTypes;
using IntegrationTests.Assertions;
using IntegrationTests.Factories;
using IntegrationTests.Fixtures;
using IntegrationTests.TestDoubles;
using Xunit;
using ProcessType = Domain.OutgoingMessages.ProcessType;
using Resolution = Energinet.DataHub.Wholesale.Contracts.Events.Resolution;

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
    public async Task Non_profiled_consumption_result_is_sent_the_energy_supplier(ProcessType completedAggregationType)
    {
        _aggregationResults.HasResultForActor(SampleData.EnergySupplierNumber, AggregationResultBuilder
            .Result()
            .WithGridArea(SampleData.GridAreaCode)
            .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod)
            .WithResolution(SampleData.Resolution)
            .Build());

        await AggregationResultsAreRetrieved(completedAggregationType);

        var outgoingMessage = await OutgoingMessageAsync(MarketRole.EnergySupplier, completedAggregationType);
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
    public async Task Total_flex_consumption_is_sent_to_the_grid_operator()
    {
        var @event = new CalculationResultCompleted()
        {
            ProcessType = Energinet.DataHub.Wholesale.Contracts.Events.ProcessType.BalanceFixing,
            Resolution = Resolution.Quarter,
            BatchId = Guid.NewGuid().ToString(),
            QuantityUnit = QuantityUnit.Kwh,
            AggregationPerGridarea = new AggregationPerGridArea() { GridAreaCode = SampleData.GridAreaCode, },
            PeriodStartUtc = Timestamp.FromDateTime(DateTime.UtcNow),
            PeriodEndUtc = Timestamp.FromDateTime(DateTime.UtcNow),
            TimeSeriesType = TimeSeriesType.FlexConsumption,
            TimeSeriesPoints =
            {
                new TimeSeriesPoint()
                {
                    Time = Timestamp.FromDateTime(DateTime.UtcNow),
                    Quantity = new DecimalValue() { Nanos = 1, Units = 1 },
                    QuantityQuality = QuantityQuality.Measured,
                },
            },
        };

        await HavingReceivedIntegrationEventAsync("CalculationResultCompleted", @event).ConfigureAwait(false);

        var message = await OutgoingMessageAsync(
            MarketRole.MeteredDataResponsible, ProcessType.BalanceFixing);
        message.HasReceiverId(SampleData.GridOperatorNumber)
            .HasReceiverRole(MarketRole.MeteredDataResponsible.Name)
            .HasSenderRole(MarketRole.MeteringDataAdministrator.Name)
            .HasSenderId(DataHubDetails.IdentificationNumber.Value)
            .HasMessageRecordValue<TimeSeries>(x => x.MeteringPointType, MeteringPointType.Consumption.Name)
            .HasMessageRecordValue<TimeSeries>(property => property.SettlementType!, SettlementType.Flex.Name);
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
            .WithMeteringPointType(MeteringPointType.Production)
            .Build());

        await AggregationResultsAreRetrieved(completedAggregationType);

        var message = await OutgoingMessageAsync(
            MarketRole.MeteredDataResponsible, completedAggregationType);
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
            .HasMessageRecordValue<TimeSeries>(x => x.Point[0].Quality!, Quality.Missing.Name);
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

        await AggregationResultsAreRetrieved(completedAggregationType);

        var outgoingMessage = await OutgoingMessageAsync(
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

    [Theory]
    [MemberData(nameof(AggregationProcessTypes))]
    public async Task Total_non_profiled_consumption_result_is_sent_to_the_balance_responsible(ProcessType completedAggregationType)
    {
        _aggregationResults.HasResultForActor(
            SampleData.BalanceResponsibleNumber,
            AggregationResultBuilder.Result()
                .WithGridArea(SampleData.GridAreaCode)
                .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod)
                .WithResolution(SampleData.Resolution)
                .WithMeteringPointType(MeteringPointType.Consumption)
                .WithSettlementMethod(SettlementType.NonProfiled)
                .WithReceivingActorNumber(SampleData.BalanceResponsibleNumber)
                .WithReceivingActorRole(MarketRole.BalanceResponsible)
                .Build());

        await AggregationResultsAreRetrieved(completedAggregationType);

        var outgoingMessage = await OutgoingMessageAsync(
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
                null!);
    }

    private async Task AggregationResultsAreRetrieved(ProcessType completedAggregationType)
    {
        await RetrieveResults(completedAggregationType).ConfigureAwait(false);
        await HavingProcessedInternalTasksAsync().ConfigureAwait(false);
    }

    private async Task<AssertOutgoingMessage> OutgoingMessageAsync(MarketRole roleOfReceiver, ProcessType completedAggregationType)
    {
        return await AssertOutgoingMessage.OutgoingMessageAsync(
            MessageType.NotifyAggregatedMeasureData.Name,
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
