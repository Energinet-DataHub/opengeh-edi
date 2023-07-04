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
using Domain.Actors;
using Domain.Documents;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Domain.Transactions.Aggregations;
using Energinet.DataHub.Wholesale.Contracts.Events;
using IntegrationTests.Assertions;
using IntegrationTests.Factories;
using IntegrationTests.Fixtures;
using Xunit;
using Resolution = Energinet.DataHub.Wholesale.Contracts.Events.Resolution;

namespace IntegrationTests.Application.Transactions.Aggregations;

public class WhenAnAggregationResultIsAvailableTests : TestBase
{
    private readonly CalculationResultCompletedEventBuilder _eventBuilder = new();

    public WhenAnAggregationResultIsAvailableTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    [Fact]
    public async Task Non_profiled_consumption_result_is_sent_the_energy_supplier()
    {
        _eventBuilder
            .WithProcessType(Energinet.DataHub.Wholesale.Contracts.Events.ProcessType.BalanceFixing)
            .AggregatedBy(SampleData.GridAreaCode, null, SampleData.EnergySupplierNumber.Value)
            .ResultOf(TimeSeriesType.NonProfiledConsumption)
            .WithResolution(Resolution.Quarter)
            .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod);

        await HavingReceivedIntegrationEventAsync(CalculationResultCompleted.MessageName, _eventBuilder.Build()).ConfigureAwait(false);

        var outgoingMessage = await OutgoingMessageAsync(MarketRole.EnergySupplier, BusinessReason.BalanceFixing);
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
    public async Task Total_non_profiled_consumption_is_sent_to_the_grid_operator()
    {
        _eventBuilder
            .WithProcessType(Energinet.DataHub.Wholesale.Contracts.Events.ProcessType.BalanceFixing)
            .WithResolution(Resolution.Quarter)
            .WithMeasurementUnit(QuantityUnit.Kwh)
            .AggregatedBy(SampleData.GridAreaCode, null, null)
            .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod)
            .ResultOf(TimeSeriesType.NonProfiledConsumption);

        await HavingReceivedIntegrationEventAsync(CalculationResultCompleted.MessageName, _eventBuilder.Build()).ConfigureAwait(false);

        var message = await OutgoingMessageAsync(
            MarketRole.MeteredDataResponsible, BusinessReason.BalanceFixing);
        message.HasReceiverId(SampleData.GridOperatorNumber)
            .HasReceiverRole(MarketRole.MeteredDataResponsible.Name)
            .HasSenderRole(MarketRole.MeteringDataAdministrator.Name)
            .HasSenderId(DataHubDetails.IdentificationNumber.Value)
            .HasMessageRecordValue<TimeSeries>(x => x.MeteringPointType, MeteringPointType.Consumption.Name)
            .HasMessageRecordValue<TimeSeries>(property => property.SettlementType!, SettlementType.NonProfiled.Name);
    }

    [Fact]
    public async Task Total_production_result_is_sent_to_the_grid_operator()
    {
        _eventBuilder
            .WithProcessType(Energinet.DataHub.Wholesale.Contracts.Events.ProcessType.BalanceFixing)
            .WithResolution(Resolution.Quarter)
            .WithMeasurementUnit(QuantityUnit.Kwh)
            .AggregatedBy(SampleData.GridAreaCode, null, null)
            .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod)
            .ResultOf(TimeSeriesType.Production);

        await HavingReceivedIntegrationEventAsync(CalculationResultCompleted.MessageName, _eventBuilder.Build()).ConfigureAwait(false);

        var message = await OutgoingMessageAsync(
            MarketRole.MeteredDataResponsible, BusinessReason.BalanceFixing);
        message.HasReceiverId(SampleData.GridOperatorNumber)
            .HasReceiverRole(MarketRole.MeteredDataResponsible.Name)
            .HasSenderRole(MarketRole.MeteringDataAdministrator.Name)
            .HasSenderId(DataHubDetails.IdentificationNumber.Value)
            .HasMessageRecordValue<TimeSeries>(x => x.GridAreaCode, SampleData.GridAreaCode)
            .HasMessageRecordValue<TimeSeries>(x => x.Resolution, Domain.Transactions.Aggregations.Resolution.QuarterHourly.Name)
            .HasMessageRecordValue<TimeSeries>(x => x.MeasureUnitType, MeasurementUnit.Kwh.Name)
            .HasMessageRecordValue<TimeSeries>(x => x.MeteringPointType, MeteringPointType.Production.Name)
            .HasMessageRecordValue<TimeSeries>(x => x.Period.Start, SampleData.StartOfPeriod)
            .HasMessageRecordValue<TimeSeries>(x => x.Period.End, SampleData.EndOfPeriod);
    }

    [Fact]
    public async Task Consumption_per_energy_supplier_result_is_sent_to_the_balance_responsible()
    {
        _eventBuilder
            .WithProcessType(Energinet.DataHub.Wholesale.Contracts.Events.ProcessType.BalanceFixing)
            .WithResolution(Resolution.Quarter)
            .WithMeasurementUnit(QuantityUnit.Kwh)
            .AggregatedBy(SampleData.GridAreaCode, SampleData.BalanceResponsibleNumber.Value, SampleData.EnergySupplierNumber.Value)
            .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod)
            .ResultOf(TimeSeriesType.NonProfiledConsumption);

        await HavingReceivedIntegrationEventAsync(CalculationResultCompleted.MessageName, _eventBuilder.Build()).ConfigureAwait(false);

        var outgoingMessage = await OutgoingMessageAsync(
            MarketRole.BalanceResponsible,
            BusinessReason.BalanceFixing);
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

    [Fact]
    public async Task Total_non_profiled_consumption_result_is_sent_to_the_balance_responsible()
    {
        _eventBuilder
            .WithProcessType(Energinet.DataHub.Wholesale.Contracts.Events.ProcessType.BalanceFixing)
            .WithResolution(Resolution.Quarter)
            .WithMeasurementUnit(QuantityUnit.Kwh)
            .AggregatedBy(SampleData.GridAreaCode, SampleData.BalanceResponsibleNumber.Value, null)
            .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod)
            .ResultOf(TimeSeriesType.NonProfiledConsumption);

        await HavingReceivedIntegrationEventAsync(CalculationResultCompleted.MessageName, _eventBuilder.Build()).ConfigureAwait(false);

        var outgoingMessage = await OutgoingMessageAsync(
            MarketRole.BalanceResponsible,
            BusinessReason.BalanceFixing);
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

    [Theory]
    [InlineData(Energinet.DataHub.Wholesale.Contracts.Events.ProcessType.BalanceFixing, nameof(BusinessReason.BalanceFixing), TimeSeriesType.NetExchangePerGa)]
    [InlineData(Energinet.DataHub.Wholesale.Contracts.Events.ProcessType.BalanceFixing, nameof(BusinessReason.BalanceFixing), TimeSeriesType.NetExchangePerNeighboringGa)]
    [InlineData(Energinet.DataHub.Wholesale.Contracts.Events.ProcessType.Aggregation, nameof(BusinessReason.PreliminaryAggregation), TimeSeriesType.NetExchangePerGa)]
    [InlineData(Energinet.DataHub.Wholesale.Contracts.Events.ProcessType.Aggregation, nameof(BusinessReason.PreliminaryAggregation), TimeSeriesType.NetExchangePerNeighboringGa)]
    public async Task Exchange_is_sent_to_the_grid_operator(ProcessType processType, string businessReason, TimeSeriesType timeSeriesType)
    {
        _eventBuilder
            .WithProcessType(processType)
            .WithResolution(Resolution.Quarter)
            .WithMeasurementUnit(QuantityUnit.Kwh)
            .AggregatedBy(SampleData.GridAreaCode, null, null)
            .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod)
            .ResultOf(timeSeriesType);

        await HavingReceivedIntegrationEventAsync(CalculationResultCompleted.MessageName, _eventBuilder.Build()).ConfigureAwait(false);

        var message = await OutgoingMessageAsync(MarketRole.MeteredDataResponsible, BusinessReason.From(businessReason));
        message.HasReceiverId(SampleData.GridOperatorNumber)
            .HasReceiverRole(MarketRole.MeteredDataResponsible.Name)
            .HasSenderRole(MarketRole.MeteringDataAdministrator.Name)
            .HasSenderId(DataHubDetails.IdentificationNumber.Value)
            .HasBusinessReason(businessReason)
            .HasMessageRecordValue<TimeSeries>(x => x.MeteringPointType, MeteringPointType.Exchange.Name);
    }

    [Theory]
    [InlineData(Energinet.DataHub.Wholesale.Contracts.Events.ProcessType.BalanceFixing, nameof(BusinessReason.BalanceFixing), TimeSeriesType.TotalConsumption)]
    [InlineData(Energinet.DataHub.Wholesale.Contracts.Events.ProcessType.Aggregation, nameof(BusinessReason.PreliminaryAggregation), TimeSeriesType.TotalConsumption)]
    public async Task Total_consumption_is_sent_to_the_grid_operator(ProcessType processType, string businessReason, TimeSeriesType timeSeriesType)
    {
        _eventBuilder
            .WithProcessType(processType)
            .WithResolution(Resolution.Quarter)
            .WithMeasurementUnit(QuantityUnit.Kwh)
            .AggregatedBy(SampleData.GridAreaCode, null, null)
            .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod)
            .ResultOf(timeSeriesType);

        await HavingReceivedIntegrationEventAsync(CalculationResultCompleted.MessageName, _eventBuilder.Build()).ConfigureAwait(false);

        var message = await OutgoingMessageAsync(MarketRole.MeteredDataResponsible, BusinessReason.From(businessReason));
        message.HasReceiverId(SampleData.GridOperatorNumber)
            .HasReceiverRole(MarketRole.MeteredDataResponsible.Name)
            .HasSenderRole(MarketRole.MeteringDataAdministrator.Name)
            .HasSenderId(DataHubDetails.IdentificationNumber.Value)
            .HasBusinessReason(businessReason)
            .HasMessageRecordValue<TimeSeries>(x => x.MeteringPointType, MeteringPointType.Consumption.Name);
    }

    private async Task<AssertOutgoingMessage> OutgoingMessageAsync(MarketRole roleOfReceiver, BusinessReason completedAggregationType)
    {
        return await AssertOutgoingMessage.OutgoingMessageAsync(
            DocumentType.NotifyAggregatedMeasureData.Name,
            completedAggregationType.Name,
            roleOfReceiver,
            GetService<IDatabaseConnectionFactory>()).ConfigureAwait(false);
    }
}
