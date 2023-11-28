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
using System.Threading.Tasks;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.Core.Messaging.Communication.Subscriber;
using Energinet.DataHub.EDI.BuildingBlocks.Domain;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Actors;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.IntegrationTests.Assertions;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.Process.Domain.Transactions.Aggregations.OutgoingMessage;
using Energinet.DataHub.Wholesale.Contracts.Events;
using Xunit;
using Resolution = Energinet.DataHub.Wholesale.Contracts.Events.Resolution;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.Transactions.Aggregations;

public class WhenAnAggregationResultIsAvailableTests : TestBase
{
    private readonly CalculationResultCompletedEventBuilder _eventBuilder = new();
    private readonly GridAreaBuilder _gridAreaBuilder = new();

    public WhenAnAggregationResultIsAvailableTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    [Fact]
    public async Task Non_profiled_consumption_result_is_sent_the_energy_supplier()
    {
        _gridAreaBuilder
            .WithGridAreaCode(SampleData.GridAreaCode)
            .WithActorNumber(SampleData.GridOperatorNumber)
            .Store(GetService<B2BContext>());

        _eventBuilder
            .WithProcessType(ProcessType.BalanceFixing)
            .AggregatedBy(SampleData.GridAreaCode, null, SampleData.EnergySupplierNumber.Value)
            .ResultOf(TimeSeriesType.NonProfiledConsumption)
            .WithResolution(Resolution.Quarter)
            .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod);

        await HavingReceivedAndHandledIntegrationEventAsync(CalculationResultCompleted.EventName, _eventBuilder.Build());

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
        _gridAreaBuilder
            .WithGridAreaCode(SampleData.GridAreaCode)
            .WithActorNumber(SampleData.GridOperatorNumber)
            .Store(GetService<B2BContext>());

        _eventBuilder
            .WithProcessType(ProcessType.BalanceFixing)
            .WithResolution(Resolution.Quarter)
            .WithMeasurementUnit(QuantityUnit.Kwh)
            .AggregatedBy(SampleData.GridAreaCode, null, null)
            .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod)
            .ResultOf(TimeSeriesType.NonProfiledConsumption);

        await HavingReceivedAndHandledIntegrationEventAsync(CalculationResultCompleted.EventName, _eventBuilder.Build());

        var message = await OutgoingMessageAsync(
            MarketRole.MeteredDataResponsible, BusinessReason.BalanceFixing);
        message.HasReceiverId(SampleData.GridOperatorNumber.Value)
            .HasReceiverRole(MarketRole.MeteredDataResponsible.Name)
            .HasSenderRole(MarketRole.MeteringDataAdministrator.Name)
            .HasSenderId(DataHubDetails.IdentificationNumber.Value)
            .HasMessageRecordValue<TimeSeries>(x => x.MeteringPointType, MeteringPointType.Consumption.Name)
            .HasMessageRecordValue<TimeSeries>(property => property.SettlementType!, SettlementType.NonProfiled.Name);
    }

    [Fact]
    public async Task Total_production_result_is_sent_to_the_grid_operator()
    {
        _gridAreaBuilder
            .WithGridAreaCode(SampleData.GridAreaCode)
            .WithActorNumber(SampleData.GridOperatorNumber)
            .Store(GetService<B2BContext>());

        _eventBuilder
            .WithProcessType(ProcessType.BalanceFixing)
            .WithResolution(Resolution.Quarter)
            .WithMeasurementUnit(QuantityUnit.Kwh)
            .AggregatedBy(SampleData.GridAreaCode, null, null)
            .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod)
            .ResultOf(TimeSeriesType.Production);

        await HavingReceivedAndHandledIntegrationEventAsync(CalculationResultCompleted.EventName, _eventBuilder.Build());

        var message = await OutgoingMessageAsync(
            MarketRole.MeteredDataResponsible, BusinessReason.BalanceFixing);
        message.HasReceiverId(SampleData.GridOperatorNumber.Value)
            .HasReceiverRole(MarketRole.MeteredDataResponsible.Name)
            .HasSenderRole(MarketRole.MeteringDataAdministrator.Name)
            .HasSenderId(DataHubDetails.IdentificationNumber.Value)
            .HasMessageRecordValue<TimeSeries>(x => x.GridAreaCode, SampleData.GridAreaCode)
            .HasMessageRecordValue<TimeSeries>(x => x.Resolution, BuildingBlocks.Domain.Models.Resolution.QuarterHourly.Name)
            .HasMessageRecordValue<TimeSeries>(x => x.MeasureUnitType, MeasurementUnit.Kwh.Name)
            .HasMessageRecordValue<TimeSeries>(x => x.MeteringPointType, MeteringPointType.Production.Name)
            .HasMessageRecordValue<TimeSeries>(x => x.Period.Start, SampleData.StartOfPeriod)
            .HasMessageRecordValue<TimeSeries>(x => x.Period.End, SampleData.EndOfPeriod);
    }

    [Fact]
    public async Task Consumption_per_energy_supplier_result_is_sent_to_the_balance_responsible()
    {
        _gridAreaBuilder
            .WithGridAreaCode(SampleData.GridAreaCode)
            .WithActorNumber(SampleData.BalanceResponsibleNumber)
            .Store(GetService<B2BContext>());

        _eventBuilder
            .WithProcessType(ProcessType.BalanceFixing)
            .WithResolution(Resolution.Quarter)
            .WithMeasurementUnit(QuantityUnit.Kwh)
            .AggregatedBy(SampleData.GridAreaCode, SampleData.BalanceResponsibleNumber.Value, SampleData.EnergySupplierNumber.Value)
            .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod)
            .ResultOf(TimeSeriesType.NonProfiledConsumption);

        await HavingReceivedAndHandledIntegrationEventAsync(CalculationResultCompleted.EventName, _eventBuilder.Build());

        var outgoingMessage = await OutgoingMessageAsync(
            MarketRole.BalanceResponsibleParty,
            BusinessReason.BalanceFixing);
        outgoingMessage
            .HasReceiverId(SampleData.BalanceResponsibleNumber.Value)
            .HasReceiverRole(MarketRole.BalanceResponsibleParty.Name)
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
        _gridAreaBuilder
            .WithGridAreaCode(SampleData.GridAreaCode)
            .WithActorNumber(SampleData.BalanceResponsibleNumber)
            .Store(GetService<B2BContext>());

        _eventBuilder
            .WithProcessType(ProcessType.BalanceFixing)
            .WithResolution(Resolution.Quarter)
            .WithMeasurementUnit(QuantityUnit.Kwh)
            .AggregatedBy(SampleData.GridAreaCode, SampleData.BalanceResponsibleNumber.Value, null)
            .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod)
            .ResultOf(TimeSeriesType.NonProfiledConsumption);

        await HavingReceivedAndHandledIntegrationEventAsync(CalculationResultCompleted.EventName, _eventBuilder.Build());

        var outgoingMessage = await OutgoingMessageAsync(
            MarketRole.BalanceResponsibleParty,
            BusinessReason.BalanceFixing);
        outgoingMessage
            .HasReceiverId(SampleData.BalanceResponsibleNumber.Value)
            .HasReceiverRole(MarketRole.BalanceResponsibleParty.Name)
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
    [InlineData(ProcessType.BalanceFixing, nameof(BusinessReason.BalanceFixing), TimeSeriesType.NetExchangePerGa)]
    [InlineData(ProcessType.BalanceFixing, nameof(BusinessReason.BalanceFixing), TimeSeriesType.NetExchangePerNeighboringGa)]
    [InlineData(ProcessType.Aggregation, nameof(BusinessReason.PreliminaryAggregation), TimeSeriesType.NetExchangePerGa)]
    [InlineData(ProcessType.Aggregation, nameof(BusinessReason.PreliminaryAggregation), TimeSeriesType.NetExchangePerNeighboringGa)]
    public async Task Exchange_is_sent_to_the_grid_operator(ProcessType processType, string businessReasonName, TimeSeriesType timeSeriesType)
    {
        var businessReason = BusinessReason.FromName(businessReasonName);
        _gridAreaBuilder
            .WithGridAreaCode(SampleData.GridAreaCode)
            .WithActorNumber(SampleData.GridOperatorNumber)
            .Store(GetService<B2BContext>());

        _eventBuilder
            .WithProcessType(processType)
            .WithResolution(Resolution.Quarter)
            .WithMeasurementUnit(QuantityUnit.Kwh)
            .AggregatedBy(SampleData.GridAreaCode, null, null)
            .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod)
            .ResultOf(timeSeriesType);

        await HavingReceivedAndHandledIntegrationEventAsync(CalculationResultCompleted.EventName, _eventBuilder.Build());

        var message = await OutgoingMessageAsync(MarketRole.MeteredDataResponsible, businessReason);
        message.HasReceiverId(SampleData.GridOperatorNumber.Value)
            .HasReceiverRole(MarketRole.MeteredDataResponsible.Name)
            .HasSenderRole(MarketRole.MeteringDataAdministrator.Name)
            .HasSenderId(DataHubDetails.IdentificationNumber.Value)
            .HasBusinessReason(businessReason)
            .HasMessageRecordValue<TimeSeries>(x => x.MeteringPointType, MeteringPointType.Exchange.Name);
    }

    [Theory]
    [InlineData(ProcessType.BalanceFixing, nameof(BusinessReason.BalanceFixing), TimeSeriesType.TotalConsumption)]
    [InlineData(ProcessType.Aggregation, nameof(BusinessReason.PreliminaryAggregation), TimeSeriesType.TotalConsumption)]
    public async Task Total_consumption_is_sent_to_the_grid_operator(ProcessType processType, string businessReasonName, TimeSeriesType timeSeriesType)
    {
        var businessReason = BusinessReason.FromName(businessReasonName);
        _gridAreaBuilder
            .WithGridAreaCode(SampleData.GridAreaCode)
            .WithActorNumber(SampleData.GridOperatorNumber)
            .Store(GetService<B2BContext>());

        _eventBuilder
            .WithProcessType(processType)
            .WithResolution(Resolution.Quarter)
            .WithMeasurementUnit(QuantityUnit.Kwh)
            .AggregatedBy(SampleData.GridAreaCode, null, null)
            .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod)
            .ResultOf(timeSeriesType);

        await HavingReceivedAndHandledIntegrationEventAsync(CalculationResultCompleted.EventName, _eventBuilder.Build());

        var message = await OutgoingMessageAsync(MarketRole.MeteredDataResponsible, businessReason);
        message.HasReceiverId(SampleData.GridOperatorNumber.Value)
            .HasReceiverRole(MarketRole.MeteredDataResponsible.Name)
            .HasSenderRole(MarketRole.MeteringDataAdministrator.Name)
            .HasSenderId(DataHubDetails.IdentificationNumber.Value)
            .HasBusinessReason(businessReason)
            .HasMessageRecordValue<TimeSeries>(x => x.MeteringPointType, MeteringPointType.Consumption.Name);
    }

    private async Task<AssertOutgoingMessage> OutgoingMessageAsync(MarketRole roleOfReceiver, BusinessReason businessReason)
    {
        return await AssertOutgoingMessage.OutgoingMessageAsync(
            DocumentType.NotifyAggregatedMeasureData.Name,
            businessReason.Name,
            roleOfReceiver,
            GetService<IDatabaseConnectionFactory>());
    }

    private async Task HavingReceivedAndHandledIntegrationEventAsync(string eventType, CalculationResultCompleted calculationResultCompleted)
    {
        var integrationEventHandler = GetService<IIntegrationEventHandler>();

        var integrationEvent = new IntegrationEvent(Guid.NewGuid(), eventType, 1, calculationResultCompleted);

        await integrationEventHandler.HandleAsync(integrationEvent).ConfigureAwait(false);
    }
}
