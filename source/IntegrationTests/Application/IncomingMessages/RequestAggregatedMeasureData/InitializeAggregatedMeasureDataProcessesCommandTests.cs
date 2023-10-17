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
using Microsoft.EntityFrameworkCore;
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
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage));

        // Assert
        var process = GetProcess(marketMessage.SenderNumber);
        Assert.Equal(marketMessage.Series.First().Id, process!.BusinessTransactionId.Id);
        AssertProcessState(process, AggregatedMeasureDataProcess.State.Initialized);
    }

    [Fact]
    public async Task Duplicated_transaction_id_across_commands_one_aggregated_measure_data_process_is_created()
    {
        // Arrange
        var marketMessage01 = MessageBuilder()
            .SetTransactionId("d0100662-1e08-477a-94a8-0f02d52be925")
            .Build();

        var marketMessage02 = MessageBuilder()
            .SetTransactionId("d0100662-1e08-477a-94a8-0f02d52be925")
            .Build();

        // Act
        var task01 = InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage01));
        var task02 = InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage02));

        try
        {
            await Task.WhenAll(task01, task02);
        }
        catch (DbUpdateException e)
        {
            // Assert
            Assert.Contains("Violation of PRIMARY KEY constraint", e.InnerException?.Message, StringComparison.InvariantCulture);
        }

        var processes = GetProcesses(marketMessage01.SenderNumber);
        Assert.Single(processes);
        var process = processes.First();
        Assert.Equal(marketMessage01.Series.First().Id, process!.BusinessTransactionId.Id);
        AssertProcessState(process, AggregatedMeasureDataProcess.State.Initialized);
    }

    [Fact]
    public async Task Duplicated_message_id_across_commands_one_aggregated_measure_data_process_is_created()
    {
        // Arrange
        var marketMessage01 = MessageBuilder()
            .SetMessageId("d0100662-1e08-477a-94a8-0f02d52be924")
            .Build();

        var marketMessage02 = MessageBuilder()
            .SetMessageId("d0100662-1e08-477a-94a8-0f02d52be924")
            .Build();

        // Act
        var task01 = InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage01));
        var task02 = InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage02));

        var tasks = new[] { task01, task02 };

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (DbUpdateException e)
        {
            // Assert
            Assert.Contains("Violation of PRIMARY KEY constraint", e.InnerException?.Message, StringComparison.InvariantCulture);
        }

        // Assert
        var processes = GetProcesses(marketMessage01.SenderNumber).ToList();

        Assert.Single(processes);

        var taskStatuses = tasks.Select(t => t.Status).ToList();
        Assert.Single(taskStatuses.Where(status => status == TaskStatus.RanToCompletion));
        Assert.Single(taskStatuses.Where(status => status == TaskStatus.Faulted));

        var completedTaskIndex = taskStatuses.FindIndex(status => status == TaskStatus.RanToCompletion);
        var completedTaskMessage = completedTaskIndex == 0 ? marketMessage01 : marketMessage02;

        var process = processes.First();

        Assert.Equal(completedTaskMessage.Series.First().Id, process.BusinessTransactionId.Id);
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
        await InvokeCommandAsync(new InitializeAggregatedMeasureDataProcessesCommand(marketMessage));
        var command = LoadCommand("SendAggregatedMeasureRequestToWholesale");
        var exceptedServiceBusMessageSubject = nameof(AggregatedTimeSeriesRequest);

        // Act
        await InvokeCommandAsync(command);

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

    private AggregatedMeasureDataProcess? GetProcess(string senderNumber)
    {
        return _b2BContext.AggregatedMeasureDataProcesses
            .ToList()
            .FirstOrDefault(x => x.RequestedByActorId.Value == senderNumber);
    }

    private IEnumerable<AggregatedMeasureDataProcess> GetProcesses(string senderNumber)
    {
        return _b2BContext.AggregatedMeasureDataProcesses
            .ToList()
            .Where(x => x.RequestedByActorId.Value == senderNumber);
    }
}
