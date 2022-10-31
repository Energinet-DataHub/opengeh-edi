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
using MediatR;
using Messaging.Application.Transactions.MoveIn.UpdateCustomer;
using Messaging.Domain.Transactions.MoveIn;
using Messaging.Infrastructure.Configuration.DataAccess;
using Messaging.Infrastructure.Configuration.MessageBus.RemoteBusinessServices;
using Messaging.IntegrationTests.Fixtures;
using Messaging.IntegrationTests.TestDoubles;
using NodaTime;
using Xunit;

namespace Messaging.IntegrationTests.Application.Transactions.MoveIn;

public class UpdateCustomerMasterDataTests : TestBase, IAsyncLifetime
{
    public UpdateCustomerMasterDataTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    public Task InitializeAsync()
    {
        return Scenario
            .Details(
                SampleData.TransactionId,
                SampleData.MeteringPointNumber,
                SampleData.SupplyStart,
                SampleData.CurrentEnergySupplierNumber,
                SampleData.NewEnergySupplierNumber,
                SampleData.ConsumerId,
                SampleData.ConsumerIdType,
                SampleData.ConsumerName,
                SampleData.OriginalMessageId,
                GetService<IMediator>(),
                GetService<B2BContext>())
            .BuildAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
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
        var nonExistingMeteringPointNumber = "571234567891234569";

        await Assert.ThrowsAsync<TransactionNotFoundException>(() =>
            InvokeCommandAsync(CreateCommand(nonExistingMeteringPointNumber))).ConfigureAwait(false);
    }

    private static UpdateCustomerMasterData CreateCommand(string? meteringPointNumber = null, Instant? supplyStartDate = null)
    {
        return new UpdateCustomerMasterData(
            meteringPointNumber ?? SampleData.MeteringPointNumber,
            supplyStartDate ?? SampleData.SupplyStart);
    }
}
