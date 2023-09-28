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
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Application.Configuration.Commands.Commands;
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Infrastructure.Configuration.InternalCommands;
using Energinet.DataHub.EDI.Infrastructure.Configuration.MessageBus;
using Energinet.DataHub.EDI.Infrastructure.Configuration.Serialization;
using Energinet.DataHub.EDI.Infrastructure.IncomingMessages.RequestAggregatedMeasureData;
using Energinet.DataHub.EDI.Infrastructure.Transactions.AggregatedMeasureData.Commands;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.Edi.Requests;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.IncomingMessages.RequestAggregatedMeasureData;

[IntegrationTest]
public class InitializeAggregatedMeasureDataProcessesCommandTests : TestBase
{
    private readonly B2BContext _b2BContext;
    private readonly ServiceBusSenderSpy _senderSpy;
    private readonly ServiceBusSenderFactoryStub _serviceBusClientSenderFactory;
    private readonly InternalCommandMapper _mapper;
    private readonly ISerializer _serializer;

    public InitializeAggregatedMeasureDataProcessesCommandTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _b2BContext = GetService<B2BContext>();
        _mapper = GetService<InternalCommandMapper>();
        _serializer = GetService<ISerializer>();
        _serviceBusClientSenderFactory = (ServiceBusSenderFactoryStub)GetService<IServiceBusSenderFactory>();
        _senderSpy = new ServiceBusSenderSpy("Fake");
        _serviceBusClientSenderFactory.AddSenderSpy(_senderSpy);
    }

    [Fact]
    public async Task Aggregated_measure_data_process_is_created()
    {
        // Arrange
        var marketMessage = MessageBuilder().Build();

        // Act
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage)).ConfigureAwait(false);

        // Assert
        var process = GetProcess(marketMessage.SenderNumber);
        Assert.Equal(marketMessage.Series.First().Id, process!.BusinessTransactionId.Id);
        AssertProcessState(process, AggregatedMeasureDataProcess.State.Initialized);
    }

    [Fact]
    public async Task Aggregated_measure_data_process_was_send_to_wholesale()
    {
        // Arrange
        var marketMessage =
            MessageBuilder().
                SetSenderRole(MarketRole.MeteredDataResponsible.Code).
                SetEnergySupplierId(null).
                SetBalanceResponsibleId(null).
                Build();
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage)).ConfigureAwait(false);
        var command = LoadCommand(nameof(SendAggregatedMeasureRequestToWholesale));
        var exceptedServiceBusMessageSubject = nameof(AggregatedTimeSeriesRequest);

        // Act
        await InvokeCommandAsync(command).ConfigureAwait(false);

        // Assert
        var message = _senderSpy.Message;
        var process = GetProcess(marketMessage.SenderNumber);
        Assert.NotNull(message);
        Assert.NotNull(process);
        Assert.Equal(process.ProcessId.Id.ToString(), message!.MessageId);
        Assert.Equal(exceptedServiceBusMessageSubject, message!.Subject);
        Assert.Equal(marketMessage.Series.First().Id, process!.BusinessTransactionId.Id);
        AssertProcessState(process, AggregatedMeasureDataProcess.State.Sent);
    }

    [Theory]
    [InlineData("E18", null, TimeSeriesType.Production)]
    [InlineData("E17", "", TimeSeriesType.TotalConsumption)]
    [InlineData("E17", null, TimeSeriesType.TotalConsumption)]
    [InlineData("E20", null, TimeSeriesType.NetExchangePerGa)]
    [InlineData("E17", "D01", TimeSeriesType.NonProfiledConsumption)]
    [InlineData("E17", "E02", TimeSeriesType.FlexConsumption)]
    public async Task Grid_Operator_requesting_aggregated_time_series_from_wholesale(
        string evaluationPointType,
        string? settlementMethod,
        TimeSeriesType expectedType)
    {
        // Arrange
        var marketMessage =
            MessageBuilder().
                SetMarketEvaluationPointType(evaluationPointType).
                SetMarketEvaluationSettlementMethod(settlementMethod).
                SetSenderRole(MarketRole.MeteredDataResponsible.Code).
                SetEnergySupplierId(null).
                SetBalanceResponsibleId(null).
                Build();
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage)).ConfigureAwait(false);
        var command = LoadCommand(nameof(SendAggregatedMeasureRequestToWholesale));

        // Act
        await InvokeCommandAsync(command).ConfigureAwait(false);

        // Assert
        var message = _senderSpy.Message;

        Assert.NotNull(message);

        var response = AggregatedTimeSeriesRequest.Parser.ParseFrom(message.Body!);

        Assert.Equal(expectedType, response.TimeSeriesType);
        Assert.NotNull(response.AggregationPerGridarea);
        var aggregationPerGridArea = response.AggregationPerGridarea;
        Assert.Equal(marketMessage.Series.First().MeteringGridAreaDomainId, aggregationPerGridArea.GridAreaCode);
        Assert.Equal(marketMessage.SenderNumber.Value, aggregationPerGridArea.GridResponsibleId);
    }

    [Fact]
    public async Task Grid_operator_requesting_invalid_time_series_types()
    {
        // Arrange
        var marketMessage =
            MessageBuilder().
                SetMarketEvaluationPointType("BAD").
                SetSenderRole(MarketRole.MeteredDataResponsible.Code).
                SetEnergySupplierId(null).
                SetBalanceResponsibleId(null).
                Build();
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage)).ConfigureAwait(false);
        var command = LoadCommand(nameof(SendAggregatedMeasureRequestToWholesale));
        var process = GetProcess(marketMessage.SenderNumber);

        // Act and assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => InvokeCommandAsync(command)).ConfigureAwait(false);
        Assert.NotNull(process);

        AssertProcessState(process, AggregatedMeasureDataProcess.State.Initialized);
    }

    [Theory]
    [InlineData("E18", null, TimeSeriesType.Production)]
    [InlineData("E17", "D01", TimeSeriesType.NonProfiledConsumption)]
    [InlineData("E17", "E02", TimeSeriesType.FlexConsumption)]
    public async Task Energy_supplier_requesting_aggregated_time_series_from_wholesale(
        string evaluationPointType,
        string? settlementMethod,
        TimeSeriesType expectedType)
    {
        // Arrange
        var marketMessage =
            MessageBuilder().
                SetMarketEvaluationPointType(evaluationPointType).
                SetMarketEvaluationSettlementMethod(settlementMethod).
                SetSenderRole(MarketRole.EnergySupplier.Code).
                SetEnergySupplierId("1232132132132").
                SetBalanceResponsibleId(null).
                Build();
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage)).ConfigureAwait(false);
        var command = LoadCommand(nameof(SendAggregatedMeasureRequestToWholesale));

        // Act
        await InvokeCommandAsync(command).ConfigureAwait(false);

        // Assert
        var message = _senderSpy.Message;

        Assert.NotNull(message);

        var response = AggregatedTimeSeriesRequest.Parser.ParseFrom(message.Body!);

        Assert.Equal(expectedType, response.TimeSeriesType);
        Assert.NotNull(response.AggregationPerEnergysupplierPerGridarea);

        var aggregationPerEnergySupplierPerGridArea = response.AggregationPerEnergysupplierPerGridarea;
        Assert.Equal(marketMessage.Series.First().MeteringGridAreaDomainId, aggregationPerEnergySupplierPerGridArea.GridAreaCode);
        Assert.Equal(marketMessage.Series.First().EnergySupplierMarketParticipantId, aggregationPerEnergySupplierPerGridArea.EnergySupplierId);
    }

    [Theory]
    [InlineData("E17", "")] // TimeSeriesType.TotalConsumption
    [InlineData("E17", null)] // TimeSeriesType.TotalConsumption
    [InlineData("E20", null)] // TimeSeriesType.NetExchangePerGa
    public async Task Energy_supplier_requesting_requesting_forbidding_time_series_types(
        string evaluationPointType,
        string? settlementMethod)
    {
        // Arrange
        var marketMessage =
            MessageBuilder().
                SetMarketEvaluationPointType(evaluationPointType).
                SetMarketEvaluationSettlementMethod(settlementMethod).
                SetSenderRole(MarketRole.EnergySupplier.Code).
                SetEnergySupplierId("1232132132132").
                SetBalanceResponsibleId(null).
                Build();
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage)).ConfigureAwait(false);
        var command = LoadCommand(nameof(SendAggregatedMeasureRequestToWholesale));
        var process = GetProcess(marketMessage.SenderNumber);

        // Act and assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => InvokeCommandAsync(command)).ConfigureAwait(false);
        Assert.NotNull(process);

        // Ensure that our process has not changed state
        AssertProcessState(process, AggregatedMeasureDataProcess.State.Initialized);
    }

    [Theory]
    [InlineData("E18", null, TimeSeriesType.Production)]
    [InlineData("E17", "D01", TimeSeriesType.NonProfiledConsumption)]
    [InlineData("E17", "E02", TimeSeriesType.FlexConsumption)]
    public async Task Balance_responsible_requesting_aggregated_time_series_from_wholesale(
        string evaluationPointType,
        string? settlementMethod,
        TimeSeriesType expectedType)
    {
        // Arrange
        var marketMessage =
            MessageBuilder().
                SetMarketEvaluationPointType(evaluationPointType).
                SetMarketEvaluationSettlementMethod(settlementMethod).
                SetSenderRole(MarketRole.BalanceResponsibleParty.Code).
                SetEnergySupplierId(null).
                SetBalanceResponsibleId("1232132132132").
                Build();
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage)).ConfigureAwait(false);
        var command = LoadCommand(nameof(SendAggregatedMeasureRequestToWholesale));

        // Act
        await InvokeCommandAsync(command).ConfigureAwait(false);

        // Assert
        var message = _senderSpy.Message;

        Assert.NotNull(message);

        var response = AggregatedTimeSeriesRequest.Parser.ParseFrom(message.Body!);

        Assert.Equal(expectedType, response.TimeSeriesType);
        Assert.NotNull(response.AggregationPerBalanceresponsiblepartyPerGridarea);

        var aggregationPerBalanceResponsible = response.AggregationPerBalanceresponsiblepartyPerGridarea;
        Assert.Equal(marketMessage.Series.First().MeteringGridAreaDomainId, aggregationPerBalanceResponsible.GridAreaCode);
        Assert.Equal(marketMessage.Series.First().BalanceResponsiblePartyMarketParticipantId, aggregationPerBalanceResponsible.BalanceResponsiblePartyId);
    }

    [Theory]
    [InlineData("E17", null)] // TimeSeriesType.TotalConsumption
    [InlineData("E17", "")] // TimeSeriesType.TotalConsumption
    [InlineData("E20", null)] // TimeSeriesType.NetExchangePerGa
    public async Task Balance_responsible_requesting_forbidding_time_series_types(
        string evaluationPointType,
        string? settlementMethod)
    {
        // Arrange
        var marketMessage =
            MessageBuilder().
                SetMarketEvaluationPointType(evaluationPointType).
                SetMarketEvaluationSettlementMethod(settlementMethod).
                SetSenderRole(MarketRole.BalanceResponsibleParty.Code).
                SetEnergySupplierId(null).
                SetBalanceResponsibleId("1232132132132").
                Build();
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage)).ConfigureAwait(false);
        var command = LoadCommand(nameof(SendAggregatedMeasureRequestToWholesale));
        var process = GetProcess(marketMessage.SenderNumber);

        // Act and assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => InvokeCommandAsync(command)).ConfigureAwait(false);
        Assert.NotNull(process);

        AssertProcessState(process, AggregatedMeasureDataProcess.State.Initialized);
    }

    [Theory]
    [InlineData("E18", null, TimeSeriesType.Production)]
    [InlineData("E17", "D01", TimeSeriesType.NonProfiledConsumption)]
    [InlineData("E17", "E02", TimeSeriesType.FlexConsumption)]
    public async Task Energy_supplier_per_balance_responsible_requesting_aggregated_time_series_from_wholesale(
        string evaluationPointType,
        string? settlementMethod,
        TimeSeriesType expectedType)
    {
        // Arrange
        var marketMessage =
            MessageBuilder().
                SetMarketEvaluationPointType(evaluationPointType).
                SetMarketEvaluationSettlementMethod(settlementMethod).
                SetSenderRole(MarketRole.EnergySupplier.Code).
                SetEnergySupplierId("9232132132999").
                SetBalanceResponsibleId("1232132132132").
                Build();
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage)).ConfigureAwait(false);
        var command = LoadCommand(nameof(SendAggregatedMeasureRequestToWholesale));

        // Act
        await InvokeCommandAsync(command).ConfigureAwait(false);

        // Assert
        var message = _senderSpy.Message;

        Assert.NotNull(message);

        var response = AggregatedTimeSeriesRequest.Parser.ParseFrom(message.Body!);

        Assert.Equal(expectedType, response.TimeSeriesType);
        Assert.NotNull(response.AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea);

        var aggregationPerEnergySupplierPerBalanceResponsible = response.AggregationPerEnergysupplierPerBalanceresponsiblepartyPerGridarea;
        Assert.Equal(marketMessage.Series.First().MeteringGridAreaDomainId, aggregationPerEnergySupplierPerBalanceResponsible.GridAreaCode);
        Assert.Equal(marketMessage.Series.First().EnergySupplierMarketParticipantId, aggregationPerEnergySupplierPerBalanceResponsible.EnergySupplierId);
        Assert.Equal(marketMessage.Series.First().BalanceResponsiblePartyMarketParticipantId, aggregationPerEnergySupplierPerBalanceResponsible.BalanceResponsiblePartyId);
    }

    [Theory]
    [InlineData("E17", null)] // TimeSeriesType.TotalConsumption
    [InlineData("E17", "")] // TimeSeriesType.TotalConsumption
    [InlineData("E20", null)] // TimeSeriesType.NetExchangePerGa
    public async Task Energy_supplier_per_balance_responsible_requesting_forbidding_time_series_types(
        string evaluationPointType,
        string? settlementMethod)
    {
        // Arrange
        var marketMessage =
            MessageBuilder().
                SetMarketEvaluationPointType(evaluationPointType).
                SetMarketEvaluationSettlementMethod(settlementMethod).
                SetSenderRole(MarketRole.EnergySupplier.Code).
                SetEnergySupplierId("9232132132999").
                SetBalanceResponsibleId("1232132132132").
                Build();
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage)).ConfigureAwait(false);
        var command = LoadCommand(nameof(SendAggregatedMeasureRequestToWholesale));
        var process = GetProcess(marketMessage.SenderNumber);

        // Act and assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => InvokeCommandAsync(command)).ConfigureAwait(false);
        Assert.NotNull(process);

        AssertProcessState(process, AggregatedMeasureDataProcess.State.Initialized);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _b2BContext.Dispose();
        _senderSpy.Dispose();
        _serviceBusClientSenderFactory.Dispose();
    }

    private static RequestAggregatedMeasureDataMarketDocumentBuilder MessageBuilder()
    {
        return new RequestAggregatedMeasureDataMarketDocumentBuilder();
    }

    private static void AssertProcessState(AggregatedMeasureDataProcess process, AggregatedMeasureDataProcess.State state)
    {
        var processState = typeof(AggregatedMeasureDataProcess).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(process);
        Assert.Equal(state, processState);
    }

    private InternalCommand LoadCommand(string internalCommandType)
    {
        var queuedInternalCommand = _b2BContext.QueuedInternalCommands
            .ToList()
            .First(x => x.Type == internalCommandType);

        var commandMetaData = _mapper.GetByName(queuedInternalCommand.Type);
        return (InternalCommand)_serializer.Deserialize(queuedInternalCommand.Data, commandMetaData.CommandType);
    }

    private AggregatedMeasureDataProcess? GetProcess(ActorNumber senderNumber)
    {
        return _b2BContext.AggregatedMeasureDataProcesses
            .ToList()
            .FirstOrDefault(x => x.RequestedByActorId.Value == senderNumber.Value);
    }
}
