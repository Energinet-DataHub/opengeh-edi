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
using Messaging.Application.Transactions.MoveIn.UpdateCustomer;
using Messaging.Domain.Transactions.MoveIn;
using Messaging.Infrastructure.Configuration.MessageBus.RemoteBusinessServices;
using Messaging.IntegrationTests.Fixtures;
using Messaging.IntegrationTests.TestDoubles;
using Xunit;

namespace Messaging.IntegrationTests.Application.Transactions.MoveIn;

public class UpdateCustomerMasterDataTests : TestBase
{
    public UpdateCustomerMasterDataTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    [Fact]
    public async Task Request_is_forwarded_to_business_service()
    {
        var command = CreateCommand();

        await InvokeCommandAsync(command).ConfigureAwait(false);

        var remoteBusinessRequestSpy = (RemoteBusinessServiceRequestSenderSpy<DummyRequest>)GetService<IRemoteBusinessServiceRequestSenderAdapter<DummyRequest>>();
        Assert.NotNull(remoteBusinessRequestSpy.Message);
    }

    [Fact]
    public async Task Move_in_transaction_must_exist()
    {
        await Assert.ThrowsAsync<TransactionNotFoundException>(() => InvokeCommandAsync(CreateCommand())).ConfigureAwait(false);
    }

    private static UpdateCustomerMasterData CreateCommand()
    {
        return new UpdateCustomerMasterData(SampleData.TransactionId);
    }
}
