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
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.Transactions.MoveIn.MasterDataDelivery;
using Messaging.Domain.Transactions.MoveIn;
using Messaging.Infrastructure.Configuration.DataAccess;
using Messaging.IntegrationTests.Application.IncomingMessages;
using Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace Messaging.IntegrationTests.Application.Transactions.MoveIn.MasterDataDelivery;

public class SendCustomerMasterDataToGridOperatorTests
    : TestBase, IAsyncLifetime
{
    public SendCustomerMasterDataToGridOperatorTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    public Task InitializeAsync()
    {
        return Scenario.Details(
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
            .IsEffective()
            .WithGridOperatorForMeteringPoint(
                SampleData.IdOfGridOperatorForMeteringPoint,
                SampleData.NumberOfGridOperatorForMeteringPoint)
            .BuildAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Message_is_delivered()
    {
        await InvokeCommandAsync(new SendCustomerMasterDataToGridOperator(SampleData.TransactionId)).ConfigureAwait(false);

        AssertTransaction.Transaction(SampleData.TransactionId, GetService<IDbConnectionFactory>())
            .HasCustomerMasterDataSentToGridOperatorState(MoveInTransaction.MasterDataState.Sent);
    }
}
