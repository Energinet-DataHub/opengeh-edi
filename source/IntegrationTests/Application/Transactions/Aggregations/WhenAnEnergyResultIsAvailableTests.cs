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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.Core.Messaging.Communication.Subscriber;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.DataHub;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;
using Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.EventProcessors;
using Energinet.DataHub.EDI.IntegrationTests.Assertions;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Energinet.DataHub.Wholesale.Contracts.IntegrationEvents;
using Xunit;
using Xunit.Abstractions;
using static Energinet.DataHub.Wholesale.Contracts.IntegrationEvents.EnergyResultProducedV2.Types;
using Resolution = Energinet.DataHub.Wholesale.Contracts.IntegrationEvents.EnergyResultProducedV2.Types.Resolution;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.Transactions.Aggregations;

public class WhenAnEnergyResultIsAvailableTests : TestBase
{
    private readonly EnergyResultProducedV2EventBuilder _eventBuilder = new();
    private readonly GridAreaBuilder _gridAreaBuilder = new();

    public WhenAnEnergyResultIsAvailableTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
    }

    public static IEnumerable<object[]> SupportedTimeSeriesTypes()
    {
        return EnergyResultProducedProcessorExtensions.SupportedTimeSeriesTypes().Select(e => new[] { (object)e });
    }

    public static IEnumerable<object[]> NotSupportedTimeSeriesTypes()
    {
        return Enum.GetValues<TimeSeriesType>()
            .Where(x => !EnergyResultProducedProcessorExtensions.SupportedTimeSeriesTypes().Contains(x))
            .Select(e => new[] { (object)e });
    }

    [Fact]
    public async Task Non_profiled_consumption_result_is_sent_the_energy_supplier()
    {
        await _gridAreaBuilder
            .WithGridAreaCode(SampleData.GridAreaCode)
            .WithActorNumber(SampleData.GridOperatorNumber)
            .StoreAsync(GetService<IMasterDataClient>());

        _eventBuilder
            .WithCalculationType(CalculationType.BalanceFixing)
            .AggregatedBy(SampleData.GridAreaCode, null, SampleData.EnergySupplierNumber.Value)
            .ResultOf(TimeSeriesType.NonProfiledConsumption)
            .WithResolution(Resolution.Quarter)
            .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod);

        await HavingReceivedAndHandledIntegrationEventAsync(EnergyResultProducedV2.EventName, _eventBuilder.Build());

        var outgoingMessage = await OutgoingMessageAsync(ActorRole.EnergySupplier, BusinessReason.BalanceFixing);
        outgoingMessage
            .HasReceiverId(SampleData.EnergySupplierNumber.Value)
            .HasReceiverRole(ActorRole.EnergySupplier.Code)
            .HasSenderId(DataHubDetails.DataHubActorNumber.Value)
            .HasSenderRole(ActorRole.MeteredDataAdministrator.Code)
            .HasProcessType(ProcessType.ReceiveEnergyResults)
            .HasRelationTo(null)
            .HasMessageRecordValue<EnergyResultMessageTimeSeries>(timeSeries => timeSeries.Period.Start, SampleData.StartOfPeriod)
            .HasMessageRecordValue<EnergyResultMessageTimeSeries>(timeSeries => timeSeries.Period.End, SampleData.EndOfPeriod)
            .HasMessageRecordValue<EnergyResultMessageTimeSeries>(timeSeries => timeSeries.GridAreaCode, SampleData.GridAreaCode)
            .HasMessageRecordValue<EnergyResultMessageTimeSeries>(timeSeries => timeSeries.MeteringPointType, MeteringPointType.Consumption.Name);
    }

    [Fact]
    public async Task Non_profiled_consumption_result_is_not_sent_when_feature_calculation_completed_is_enabled()
    {
        await _gridAreaBuilder
            .WithGridAreaCode(SampleData.GridAreaCode)
            .WithActorNumber(SampleData.GridOperatorNumber)
            .StoreAsync(GetService<IMasterDataClient>());

        _eventBuilder
            .WithCalculationType(CalculationType.BalanceFixing)
            .AggregatedBy(SampleData.GridAreaCode, null, SampleData.EnergySupplierNumber.Value)
            .ResultOf(TimeSeriesType.NonProfiledConsumption)
            .WithResolution(Resolution.Quarter)
            .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod);

        FeatureFlagManagerStub.EnableCalculationCompletedEvent(true);

        await HavingReceivedAndHandledIntegrationEventAsync(EnergyResultProducedV2.EventName, _eventBuilder.Build());

        await AssertOutgoingMessage.OutgoingMessageIsNullAsync(
            DocumentType.NotifyAggregatedMeasureData.Name,
            BusinessReason.BalanceFixing.Name,
            ActorRole.EnergySupplier,
            GetService<IDatabaseConnectionFactory>());
    }

    [Fact]
    public async Task Total_non_profiled_consumption_is_sent_to_the_grid_operator()
    {
        await _gridAreaBuilder
            .WithGridAreaCode(SampleData.GridAreaCode)
            .WithActorNumber(SampleData.GridOperatorNumber)
            .StoreAsync(GetService<IMasterDataClient>());

        _eventBuilder
            .WithCalculationType(CalculationType.BalanceFixing)
            .WithResolution(Resolution.Quarter)
            .WithMeasurementUnit(QuantityUnit.Kwh)
            .AggregatedBy(SampleData.GridAreaCode, null, null)
            .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod)
            .ResultOf(TimeSeriesType.NonProfiledConsumption);

        await HavingReceivedAndHandledIntegrationEventAsync(EnergyResultProducedV2.EventName, _eventBuilder.Build());

        var message = await OutgoingMessageAsync(
            ActorRole.MeteredDataResponsible, BusinessReason.BalanceFixing);
        message.HasReceiverId(SampleData.GridOperatorNumber.Value)
            .HasReceiverRole(ActorRole.MeteredDataResponsible.Code)
            .HasSenderRole(ActorRole.MeteredDataAdministrator.Code)
            .HasSenderId(DataHubDetails.DataHubActorNumber.Value)
            .HasProcessType(ProcessType.ReceiveEnergyResults)
            .HasMessageRecordValue<EnergyResultMessageTimeSeries>(x => x.MeteringPointType, MeteringPointType.Consumption.Name)
            .HasMessageRecordValue<EnergyResultMessageTimeSeries>(property => property.SettlementMethod!, SettlementMethod.NonProfiled.Name);
    }

    [Fact]
    public async Task Total_production_result_is_sent_to_the_grid_operator()
    {
        await _gridAreaBuilder
            .WithGridAreaCode(SampleData.GridAreaCode)
            .WithActorNumber(SampleData.GridOperatorNumber)
            .StoreAsync(GetService<IMasterDataClient>());

        _eventBuilder
            .WithCalculationType(CalculationType.BalanceFixing)
            .WithResolution(Resolution.Quarter)
            .WithMeasurementUnit(QuantityUnit.Kwh)
            .AggregatedBy(SampleData.GridAreaCode, null, null)
            .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod)
            .ResultOf(TimeSeriesType.Production);

        await HavingReceivedAndHandledIntegrationEventAsync(EnergyResultProducedV2.EventName, _eventBuilder.Build());

        var message = await OutgoingMessageAsync(
            ActorRole.MeteredDataResponsible, BusinessReason.BalanceFixing);
        message.HasReceiverId(SampleData.GridOperatorNumber.Value)
            .HasReceiverRole(ActorRole.MeteredDataResponsible.Code)
            .HasSenderRole(ActorRole.MeteredDataAdministrator.Code)
            .HasSenderId(DataHubDetails.DataHubActorNumber.Value)
            .HasProcessType(ProcessType.ReceiveEnergyResults)
            .HasMessageRecordValue<EnergyResultMessageTimeSeries>(x => x.GridAreaCode, SampleData.GridAreaCode)
            .HasMessageRecordValue<EnergyResultMessageTimeSeries>(x => x.Resolution, BuildingBlocks.Domain.Models.Resolution.QuarterHourly.Name)
            .HasMessageRecordValue<EnergyResultMessageTimeSeries>(x => x.MeasureUnitType, MeasurementUnit.Kwh.Name)
            .HasMessageRecordValue<EnergyResultMessageTimeSeries>(x => x.MeteringPointType, MeteringPointType.Production.Name)
            .HasMessageRecordValue<EnergyResultMessageTimeSeries>(x => x.Period.Start, SampleData.StartOfPeriod)
            .HasMessageRecordValue<EnergyResultMessageTimeSeries>(x => x.Period.End, SampleData.EndOfPeriod);
    }

    [Fact]
    public async Task Consumption_per_energy_supplier_result_is_sent_to_the_balance_responsible()
    {
        await _gridAreaBuilder
            .WithGridAreaCode(SampleData.GridAreaCode)
            .WithActorNumber(SampleData.BalanceResponsibleNumber)
            .StoreAsync(GetService<IMasterDataClient>());

        _eventBuilder
            .WithCalculationType(CalculationType.BalanceFixing)
            .WithResolution(Resolution.Quarter)
            .WithMeasurementUnit(QuantityUnit.Kwh)
            .AggregatedBy(SampleData.GridAreaCode, SampleData.BalanceResponsibleNumber.Value, SampleData.EnergySupplierNumber.Value)
            .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod)
            .ResultOf(TimeSeriesType.NonProfiledConsumption);

        await HavingReceivedAndHandledIntegrationEventAsync(EnergyResultProducedV2.EventName, _eventBuilder.Build());

        var outgoingMessage = await OutgoingMessageAsync(
            ActorRole.BalanceResponsibleParty,
            BusinessReason.BalanceFixing);
        outgoingMessage
            .HasReceiverId(SampleData.BalanceResponsibleNumber.Value)
            .HasReceiverRole(ActorRole.BalanceResponsibleParty.Code)
            .HasSenderId(DataHubDetails.DataHubActorNumber.Value)
            .HasSenderRole(ActorRole.MeteredDataAdministrator.Code)
            .HasProcessType(ProcessType.ReceiveEnergyResults)
            .HasMessageRecordValue<EnergyResultMessageTimeSeries>(
                series => series.BalanceResponsibleNumber!,
                SampleData.BalanceResponsibleNumber.Value)
            .HasMessageRecordValue<EnergyResultMessageTimeSeries>(
                series => series.EnergySupplierNumber!,
                SampleData.EnergySupplierNumber.Value);
    }

    [Fact]
    public async Task Total_non_profiled_consumption_result_is_sent_to_the_balance_responsible()
    {
        await _gridAreaBuilder
            .WithGridAreaCode(SampleData.GridAreaCode)
            .WithActorNumber(SampleData.BalanceResponsibleNumber)
            .StoreAsync(GetService<IMasterDataClient>());

        _eventBuilder
            .WithCalculationType(CalculationType.BalanceFixing)
            .WithResolution(Resolution.Quarter)
            .WithMeasurementUnit(QuantityUnit.Kwh)
            .AggregatedBy(SampleData.GridAreaCode, SampleData.BalanceResponsibleNumber.Value, null)
            .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod)
            .ResultOf(TimeSeriesType.NonProfiledConsumption);

        await HavingReceivedAndHandledIntegrationEventAsync(EnergyResultProducedV2.EventName, _eventBuilder.Build());

        var outgoingMessage = await OutgoingMessageAsync(
            ActorRole.BalanceResponsibleParty,
            BusinessReason.BalanceFixing);
        outgoingMessage
            .HasReceiverId(SampleData.BalanceResponsibleNumber.Value)
            .HasReceiverRole(ActorRole.BalanceResponsibleParty.Code)
            .HasSenderId(DataHubDetails.DataHubActorNumber.Value)
            .HasSenderRole(ActorRole.MeteredDataAdministrator.Code)
            .HasProcessType(ProcessType.ReceiveEnergyResults)
            .HasMessageRecordValue<EnergyResultMessageTimeSeries>(
                series => series.BalanceResponsibleNumber!,
                SampleData.BalanceResponsibleNumber.Value)
            .HasMessageRecordValue<EnergyResultMessageTimeSeries>(
                series => series.EnergySupplierNumber!,
                null!);
    }

    [Theory]
    [InlineData(CalculationType.BalanceFixing, nameof(BusinessReason.BalanceFixing), TimeSeriesType.NetExchangePerGa)]
    [InlineData(CalculationType.BalanceFixing, nameof(BusinessReason.BalanceFixing), TimeSeriesType.NetExchangePerNeighboringGa)]
    [InlineData(CalculationType.Aggregation, nameof(BusinessReason.PreliminaryAggregation), TimeSeriesType.NetExchangePerGa)]
    [InlineData(CalculationType.Aggregation, nameof(BusinessReason.PreliminaryAggregation), TimeSeriesType.NetExchangePerNeighboringGa)]
    public async Task Exchange_is_sent_to_the_grid_operator(CalculationType calculationType, string businessReasonName, TimeSeriesType timeSeriesType)
    {
        var businessReason = BusinessReason.FromName(businessReasonName);
        await _gridAreaBuilder
            .WithGridAreaCode(SampleData.GridAreaCode)
            .WithActorNumber(SampleData.GridOperatorNumber)
            .StoreAsync(GetService<IMasterDataClient>());

        _eventBuilder
            .WithCalculationType(calculationType)
            .WithResolution(Resolution.Quarter)
            .WithMeasurementUnit(QuantityUnit.Kwh)
            .AggregatedBy(SampleData.GridAreaCode, null, null)
            .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod)
            .ResultOf(timeSeriesType);

        await HavingReceivedAndHandledIntegrationEventAsync(EnergyResultProducedV2.EventName, _eventBuilder.Build());

        var message = await OutgoingMessageAsync(ActorRole.MeteredDataResponsible, businessReason);
        message.HasReceiverId(SampleData.GridOperatorNumber.Value)
            .HasReceiverRole(ActorRole.MeteredDataResponsible.Code)
            .HasSenderRole(ActorRole.MeteredDataAdministrator.Code)
            .HasSenderId(DataHubDetails.DataHubActorNumber.Value)
            .HasBusinessReason(businessReason)
            .HasProcessType(ProcessType.ReceiveEnergyResults)
            .HasMessageRecordValue<EnergyResultMessageTimeSeries>(x => x.MeteringPointType, MeteringPointType.Exchange.Name);
    }

    [Theory]
    [InlineData(CalculationType.BalanceFixing, nameof(BusinessReason.BalanceFixing), TimeSeriesType.TotalConsumption)]
    [InlineData(CalculationType.Aggregation, nameof(BusinessReason.PreliminaryAggregation), TimeSeriesType.TotalConsumption)]
    public async Task Total_consumption_is_sent_to_the_grid_operator(CalculationType calculationType, string businessReasonName, TimeSeriesType timeSeriesType)
    {
        var businessReason = BusinessReason.FromName(businessReasonName);
        await _gridAreaBuilder
            .WithGridAreaCode(SampleData.GridAreaCode)
            .WithActorNumber(SampleData.GridOperatorNumber)
            .StoreAsync(GetService<IMasterDataClient>());

        _eventBuilder
            .WithCalculationType(calculationType)
            .WithResolution(Resolution.Quarter)
            .WithMeasurementUnit(QuantityUnit.Kwh)
            .AggregatedBy(SampleData.GridAreaCode, null, null)
            .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod)
            .ResultOf(timeSeriesType);

        await HavingReceivedAndHandledIntegrationEventAsync(EnergyResultProducedV2.EventName, _eventBuilder.Build());

        var message = await OutgoingMessageAsync(ActorRole.MeteredDataResponsible, businessReason);
        message.HasReceiverId(SampleData.GridOperatorNumber.Value)
            .HasReceiverRole(ActorRole.MeteredDataResponsible.Code)
            .HasSenderRole(ActorRole.MeteredDataAdministrator.Code)
            .HasSenderId(DataHubDetails.DataHubActorNumber.Value)
            .HasBusinessReason(businessReason)
            .HasProcessType(ProcessType.ReceiveEnergyResults)
            .HasMessageRecordValue<EnergyResultMessageTimeSeries>(x => x.MeteringPointType, MeteringPointType.Consumption.Name);
    }

    [Theory(DisplayName = nameof(Message_is_created_for_supported_time_series_type))]
    [MemberData(nameof(SupportedTimeSeriesTypes))]
    public async Task Message_is_created_for_supported_time_series_type(TimeSeriesType timeSeriesType)
    {
        var businessReason = BusinessReason.FromName(nameof(BusinessReason.BalanceFixing));
        await _gridAreaBuilder
            .WithGridAreaCode(SampleData.GridAreaCode)
            .WithActorNumber(SampleData.GridOperatorNumber)
            .StoreAsync(GetService<IMasterDataClient>());

        _eventBuilder
            .WithResolution(Resolution.Quarter)
            .WithMeasurementUnit(QuantityUnit.Kwh)
            .AggregatedBy(SampleData.GridAreaCode, null, null)
            .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod)
            .ResultOf(timeSeriesType);

        await HavingReceivedAndHandledIntegrationEventAsync(EnergyResultProducedV2.EventName, _eventBuilder.Build());

        var message = await OutgoingMessageAsync(ActorRole.MeteredDataResponsible, businessReason);
        message.HasReceiverId(SampleData.GridOperatorNumber.Value)
            .HasReceiverRole(ActorRole.MeteredDataResponsible.Code)
            .HasSenderRole(ActorRole.MeteredDataAdministrator.Code)
            .HasSenderId(DataHubDetails.DataHubActorNumber.Value)
            .HasProcessType(ProcessType.ReceiveEnergyResults)
            .HasBusinessReason(businessReason);
    }

    [Theory(DisplayName = nameof(Message_is_not_created_for_unsupported_time_series_type))]
    [MemberData(nameof(NotSupportedTimeSeriesTypes))]
    public async Task Message_is_not_created_for_unsupported_time_series_type(TimeSeriesType timeSeriesType)
    {
        var businessReason = BusinessReason.FromName(nameof(BusinessReason.BalanceFixing));
        await _gridAreaBuilder
            .WithGridAreaCode(SampleData.GridAreaCode)
            .WithActorNumber(SampleData.GridOperatorNumber)
            .StoreAsync(GetService<IMasterDataClient>());

        _eventBuilder
            .WithResolution(Resolution.Quarter)
            .WithMeasurementUnit(QuantityUnit.Kwh)
            .AggregatedBy(SampleData.GridAreaCode, null, null)
            .WithPeriod(SampleData.StartOfPeriod, SampleData.EndOfPeriod)
            .ResultOf(timeSeriesType);

        await HavingReceivedAndHandledIntegrationEventAsync(EnergyResultProducedV2.EventName, _eventBuilder.Build());

        await AssertOutgoingMessage.OutgoingMessageIsNullAsync(
            DocumentType.NotifyAggregatedMeasureData.Name,
            businessReason.Name,
            ActorRole.MeteredDataResponsible,
            GetService<IDatabaseConnectionFactory>());
    }

    private async Task<AssertOutgoingMessage> OutgoingMessageAsync(ActorRole roleOfReceiver, BusinessReason businessReason)
    {
        return await AssertOutgoingMessage.OutgoingMessageAsync(
            DocumentType.NotifyAggregatedMeasureData.Name,
            businessReason.Name,
            roleOfReceiver,
            GetService<IDatabaseConnectionFactory>(),
            GetService<IFileStorageClient>());
    }

    private Task HavingReceivedAndHandledIntegrationEventAsync(string eventType, EnergyResultProducedV2 calculationResultCompleted)
    {
        var integrationEventHandler = GetService<IIntegrationEventHandler>();

        var integrationEvent = new IntegrationEvent(Guid.NewGuid(), eventType, 1, calculationResultCompleted);

        return integrationEventHandler.HandleAsync(integrationEvent);
    }
}
