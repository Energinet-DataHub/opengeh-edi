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
using Energinet.DataHub.EDI.Infrastructure.Transactions.AggregatedMeasureData.Commands;
using Energinet.DataHub.EDI.IntegrationTests.Application.IncomingMessages;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.Edi.Requests;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.Transactions.AggregatedMeasureData;

[IntegrationTest]
public class RequestAggregatedMeasureDataTransactionTests : TestBase
{
    private readonly B2BContext _b2BContext;
    private readonly ServiceBusSenderSpy _senderSpy;
    private readonly ServiceBusSenderFactoryStub _serviceBusClientSenderFactory;
    private readonly InternalCommandMapper _mapper;
    private readonly ISerializer _serializer;

    public RequestAggregatedMeasureDataTransactionTests(DatabaseFixture databaseFixture)
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
        var incomingMessage = MessageBuilder().Build();

        // Act
        await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);

        // Assert
        var process = GetProcess(incomingMessage.MessageHeader.SenderId);
        Assert.Equal(process!.BusinessTransactionId.Id, incomingMessage.MarketActivityRecord.Id);
        AssertProcessState(process, AggregatedMeasureDataProcess.State.Initialized);
    }

    [Fact]
    public async Task Aggregated_measure_data_process_was_send_to_wholesale()
    {
        // Arrange
        var incomingMessage =
            MessageBuilder().
            SetSenderRole(MarketRole.MeteredDataResponsible.Code).
            SetEnergySupplierId(null).
            SetBalanceResponsibleId(null).
            Build();
        await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);
        var command = LoadCommand(nameof(SendAggregatedMeasureRequestToWholesale));
        var exceptedServiceBusMessageSubject = nameof(AggregatedTimeSeriesRequest);

        // Act
        await InvokeCommandAsync(command).ConfigureAwait(false);

        // Assert
        var message = _senderSpy.Message;
        var process = GetProcess(incomingMessage.MessageHeader.SenderId);
        Assert.NotNull(message);
        Assert.NotNull(process);
        Assert.Equal(process.ProcessId.Id.ToString(), message!.MessageId);
        Assert.Equal(exceptedServiceBusMessageSubject, message!.Subject);
        Assert.Equal(incomingMessage.MarketActivityRecord.Id, process.BusinessTransactionId.Id);
        AssertProcessState(process, AggregatedMeasureDataProcess.State.Sent);
    }

    [Fact]
    public async Task Grid_operator_requesting_production_from_wholesale()
    {
        // Arrange
        var incomingMessage =
            MessageBuilder().
            SetMarketEvaluationPointType("E18").
            SetSenderRole(MarketRole.MeteredDataResponsible.Code).
            SetEnergySupplierId(null).
            SetBalanceResponsibleId(null).
            Build();
        await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);
        var command = LoadCommand(nameof(SendAggregatedMeasureRequestToWholesale));

        // Act
        await InvokeCommandAsync(command).ConfigureAwait(false);

        // Assert
        var message = _senderSpy.Message;

        Assert.NotNull(message);

        var response = AggregatedTimeSeriesRequest.Parser.ParseFrom(message.Body!);

        Assert.Equal(TimeSeriesType.Production, response.TimeSeriesType);
        Assert.NotNull(response.AggregationPerGridarea);
        var aggregationPerGridArea = response.AggregationPerGridarea;
        Assert.Equal(incomingMessage.MarketActivityRecord.MeteringGridAreaDomainId, aggregationPerGridArea.GridAreaCode);
        Assert.Equal(incomingMessage.MessageHeader.SenderId, aggregationPerGridArea.GridResponsibleId);
    }

    [Fact]
    public async Task Grid_operator_requesting_net_exchange_from_wholesale()
    {
        // Arrange
        var incomingMessage =
            MessageBuilder().
            SetMarketEvaluationPointType("E20").
            SetSenderRole("MDR").
            SetEnergySupplierId(null).
            SetBalanceResponsibleId(null).
            Build();
        await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);
        var command = LoadCommand(nameof(SendAggregatedMeasureRequestToWholesale));

        // Act
        await InvokeCommandAsync(command).ConfigureAwait(false);

        // Assert
        var message = _senderSpy.Message;

        Assert.NotNull(message);

        var response = AggregatedTimeSeriesRequest.Parser.ParseFrom(message.Body!);

        Assert.Equal(TimeSeriesType.NetExchangePerGa, response.TimeSeriesType);
        Assert.NotNull(response.AggregationPerGridarea);
    }

    [Fact]
    public async Task Grid_operator_requesting_non_profiled_consumption_from_wholesale()
    {
        // Arrange
        var incomingMessage =
            MessageBuilder().
            SetMarketEvaluationPointType("E17").
            SetMarketEvaluationSettlementMethod("D01").
            SetSenderRole("MDR").
            SetEnergySupplierId(null).
            SetBalanceResponsibleId(null).
            Build();
        await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);
        var command = LoadCommand(nameof(SendAggregatedMeasureRequestToWholesale));

        // Act
        await InvokeCommandAsync(command).ConfigureAwait(false);

        // Assert
        var message = _senderSpy.Message;

        Assert.NotNull(message);

        var response = AggregatedTimeSeriesRequest.Parser.ParseFrom(message.Body!);

        Assert.Equal(TimeSeriesType.NonProfiledConsumption, response.TimeSeriesType);
        Assert.NotNull(response.AggregationPerGridarea);
    }

    [Fact]
    public async Task Grid_operator_requesting_flex_consumption_from_wholesale()
    {
        // Arrange
        var incomingMessage =
            MessageBuilder().
            SetMarketEvaluationPointType("E17").
            SetMarketEvaluationSettlementMethod("E02").
            SetSenderRole("MDR").
            SetEnergySupplierId(null).
            SetBalanceResponsibleId(null).
            Build();
        await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);
        var command = LoadCommand(nameof(SendAggregatedMeasureRequestToWholesale));

        // Act
        await InvokeCommandAsync(command).ConfigureAwait(false);

        // Assert
        var message = _senderSpy.Message;

        Assert.NotNull(message);

        var response = AggregatedTimeSeriesRequest.Parser.ParseFrom(message.Body!);

        Assert.Equal(TimeSeriesType.FlexConsumption, response.TimeSeriesType);
        Assert.NotNull(response.AggregationPerGridarea);
    }

    [Fact]
    public async Task Grid_operator_requesting_total_consumption_from_wholesale()
    {
        // Arrange
        var incomingMessage =
            MessageBuilder().
            SetMarketEvaluationPointType("E17").
            SetMarketEvaluationSettlementMethod(null).
            SetSenderRole("MDR").
            SetEnergySupplierId(null).
            SetBalanceResponsibleId(null).
            Build();
        await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);
        var command = LoadCommand(nameof(SendAggregatedMeasureRequestToWholesale));

        // Act
        await InvokeCommandAsync(command).ConfigureAwait(false);

        // Assert
        var message = _senderSpy.Message;

        Assert.NotNull(message);

        var response = AggregatedTimeSeriesRequest.Parser.ParseFrom(message.Body!);

        Assert.Equal(TimeSeriesType.TotalConsumption, response.TimeSeriesType);
        Assert.NotNull(response.AggregationPerGridarea);
    }

    [Fact]
    public async Task Grid_operator_making_invalid_request_from_wholesale()
    {
        // Arrange
        var incomingMessage =
            MessageBuilder().
            SetMarketEvaluationPointType("BAD").
            SetSenderRole(MarketRole.MeteredDataResponsible.Code).
            SetEnergySupplierId(null).
            SetBalanceResponsibleId(null).
            Build();
        await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);
        var command = LoadCommand(nameof(SendAggregatedMeasureRequestToWholesale));
        var process = GetProcess(incomingMessage.MessageHeader.SenderId);

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

    private static RequestAggregatedMeasureDataMessageBuilder MessageBuilder()
    {
        return new RequestAggregatedMeasureDataMessageBuilder();
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

    private AggregatedMeasureDataProcess? GetProcess(string senderId)
    {
        return _b2BContext.AggregatedMeasureDataProcesses
            .ToList()
            .FirstOrDefault(x => x.RequestedByActorId.Value == senderId);
    }
}
