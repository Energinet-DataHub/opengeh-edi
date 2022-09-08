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
using Energinet.DataHub.EnergySupplying.RequestResponse.Requests;
using Energinet.DataHub.MeteringPoints.RequestResponse.Requests;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Infrastructure.Configuration.MessageBus;
using Messaging.Infrastructure.Transactions.MoveIn;
using Messaging.IntegrationTests.Fixtures;
using Messaging.IntegrationTests.TestDoubles;
using Xunit;

namespace Messaging.IntegrationTests.Application.Transactions.MoveIn;

public class FetchCustomerMasterDataTests : TestBase
{
    private readonly ServiceBusSenderSpy _senderSpy;
    private readonly ServiceBusSenderFactoryStub _serviceBusClientSenderFactory;

    public FetchCustomerMasterDataTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _serviceBusClientSenderFactory = (ServiceBusSenderFactoryStub)GetService<IServiceBusSenderFactory>();
        _senderSpy = new ServiceBusSenderSpy("Fake");
        _serviceBusClientSenderFactory.AddSenderSpy(_senderSpy);
    }

    [Fact]
    public async Task Customer_master_data_request_is_dispatched()
    {
        var command = new FetchCustomerMasterData(
            Guid.NewGuid().ToString(),
            "123445611",
            Guid.NewGuid().ToString());

        await InvokeCommandAsync(command).ConfigureAwait(false);

        var dispatchedMessage = _senderSpy.Message;
        Assert.NotNull(dispatchedMessage);
        Assert.Equal(command.TransactionId, dispatchedMessage?.ApplicationProperties["TransactionId"]);
        Assert.Equal(command.BusinessProcessId, dispatchedMessage?.ApplicationProperties["BusinessProcessId"]);
        var request = CustomerMasterDataRequest.Parser.ParseFrom(dispatchedMessage?.Body);
        Assert.Equal(command.BusinessProcessId, request.Processid);
    }
}
