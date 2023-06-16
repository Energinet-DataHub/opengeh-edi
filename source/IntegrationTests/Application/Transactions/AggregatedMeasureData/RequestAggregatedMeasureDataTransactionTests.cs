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

using System.Text;
using System.Threading.Tasks;
using Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EnergySupplying.RequestResponse.Requests;
using Infrastructure.Configuration.MessageBus;
using Infrastructure.Configuration.Serialization;
using IntegrationTests.Application.IncomingMessages;
using IntegrationTests.Fixtures;
using IntegrationTests.TestDoubles;
using Xunit;
using Xunit.Categories;

namespace IntegrationTests.Application.Transactions.AggregatedMeasureData;

[IntegrationTest]
public class RequestAggregatedMeasureDataTransactionTests : TestBase
{
    private readonly ServiceBusSenderSpy _senderSpy;
    private readonly ServiceBusSenderFactoryStub _serviceBusClientSenderFactory;
    private readonly ISerializer _jsonSerializer;

    public RequestAggregatedMeasureDataTransactionTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _jsonSerializer = GetService<ISerializer>();
        _serviceBusClientSenderFactory = (ServiceBusSenderFactoryStub)GetService<IServiceBusSenderFactory>();
        _senderSpy = new ServiceBusSenderSpy("Fake");
        _serviceBusClientSenderFactory.AddSenderSpy(_senderSpy);
    }

    [Fact]
    public async Task Aggregated_measure_data_transaction_is_started_send_to_service_bus()
    {
        var incomingMessage = MessageBuilder()
            .Build();

        await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);

        var dispatchedMessage = _senderSpy.Message;
        Assert.NotNull(dispatchedMessage);
        var byteAsString = Encoding.UTF8.GetString(dispatchedMessage?.Body);
        var dispatchedAggregatedMeasureDataTransactionRequest = _jsonSerializer.Deserialize<AggregatedMeasureDataTransactionRequest>(byteAsString);
        Assert.Equal(incomingMessage.MessageHeader.MessageId, dispatchedAggregatedMeasureDataTransactionRequest.Message.MessageId);
    }

    private static RequestAggregatedMeasureDataMessageBuilder MessageBuilder()
    {
        return new RequestAggregatedMeasureDataMessageBuilder();
    }

    private async Task DisposeAsync()
    {
        await _senderSpy.DisposeAsync().ConfigureAwait(false);
        await _serviceBusClientSenderFactory.DisposeAsync().ConfigureAwait(false);
    }
}
