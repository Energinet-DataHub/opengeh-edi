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

using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Application.Configuration.Commands.Commands;
using Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.Edi.Responses;
using Infrastructure.Configuration.DataAccess;
using Infrastructure.Configuration.InternalCommands;
using Infrastructure.Configuration.MessageBus;
using Infrastructure.Configuration.Serialization;
using Infrastructure.Transactions.AggregatedMeasureData.Commands;
using IntegrationTests.Application.IncomingMessages;
using IntegrationTests.Fixtures;
using IntegrationTests.TestDoubles;
using Xunit;
using Xunit.Categories;

namespace IntegrationTests.Application.Transactions.AggregatedMeasureData;

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
        var incomingMessage = MessageBuilder().Build();
        await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);
        var command = LoadCommand(nameof(SendAggregatedMeasureRequestToWholesale));
        //TODO: Should be AggregatedTimeSeriesRequestRequest when we communicate with Wholesales.
        var exceptedServiceBusMessageSubject = nameof(AggregatedTimeSeriesRequestAccepted);

        // Act
        await InvokeCommandAsync(command).ConfigureAwait(false);

        // Assert
        var message = _senderSpy.Message;
        var process = GetProcess(incomingMessage.MessageHeader.SenderId);
        Assert.NotNull(message);
        Assert.NotNull(process);
        Assert.Equal(process.ProcessId.Id.ToString(), message!.MessageId);
        Assert.Equal(exceptedServiceBusMessageSubject, message!.Subject);
        Assert.Equal(process.BusinessTransactionId.Id, incomingMessage.MarketActivityRecord.Id);
        AssertProcessState(process, AggregatedMeasureDataProcess.State.Sent);
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
