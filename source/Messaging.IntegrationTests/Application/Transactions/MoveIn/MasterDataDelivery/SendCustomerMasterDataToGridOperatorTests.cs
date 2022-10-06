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
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.Transactions.MoveIn.MasterDataDelivery;
using Messaging.Domain.Transactions.MoveIn;
using Messaging.IntegrationTests.Application.IncomingMessages;
using Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace Messaging.IntegrationTests.Application.Transactions.MoveIn.MasterDataDelivery;

public class SendCustomerMasterDataToGridOperatorTests
    : TestBase
{
    public SendCustomerMasterDataToGridOperatorTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    [Fact]
    public async Task Message_is_delivered()
    {
        await GivenMoveInHasBeenAcceptedAsync().ConfigureAwait(false);

        await InvokeCommandAsync(new SendCustomerMasterDataToGridOperator(SampleData.TransactionId)).ConfigureAwait(false);

        AssertTransaction.Transaction(SampleData.TransactionId, GetService<IDbConnectionFactory>())
            .HasCustomerMasterDataSentToGridOperatorState(MoveInTransaction.MasterDataState.Sent);
    }

    private Task GivenMoveInHasBeenAcceptedAsync()
    {
        var message = new IncomingMessageBuilder()
            .WithSenderId(SampleData.SenderId)
            .WithMessageId(SampleData.OriginalMessageId)
            .WithTransactionId(SampleData.TransactionId)
            .WithMarketEvaluationPointId(SampleData.MarketEvaluationPointId)
            .WithEnergySupplierId(SampleData.NewEnergySupplierNumber)
            .WithEffectiveDate(SampleData.SupplyStart)
            .WithConsumerId(SampleData.ConsumerId)
            .WithConsumerName(SampleData.ConsumerName)
            .Build();
        return InvokeCommandAsync(message);
    }
}
