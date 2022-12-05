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
using Dapper;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.OutgoingMessages.Dequeue;
using Messaging.Application.OutgoingMessages.Peek;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.Peek;
using Messaging.IntegrationTests.Application.IncomingMessages;
using Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace Messaging.IntegrationTests.Application.OutgoingMessages;

public class WhenADequeueIsRequestedTests : TestBase
{
    public WhenADequeueIsRequestedTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    [Fact]
    public async Task Dequeue_is_unsuccessful_when_bundle_does_not_exist()
    {
        var dequeueResult = await InvokeCommandAsync(new DequeueRequest(Guid.NewGuid())).ConfigureAwait(false);

        Assert.False(dequeueResult.Success);
    }

    [Fact]
    public async Task Dequeue_is_Successful()
    {
        await GivenAMoveInTransactionHasBeenAccepted().ConfigureAwait(false);
        var peekResult = await InvokeCommandAsync(new PeekRequest(
            ActorNumber.Create(SampleData.NewEnergySupplierNumber),
            MessageCategory.MasterData,
            MarketRole.EnergySupplier)).ConfigureAwait(false);

        var dequeueResult = await InvokeCommandAsync(new DequeueRequest(peekResult.MessageId.GetValueOrDefault())).ConfigureAwait(false);

        var found = await GetService<IDbConnectionFactory>().GetOpenConnection()
            .QuerySingleOrDefaultAsync("SELECT * FROM [B2B].BundleStore")
            .ConfigureAwait(false);

        Assert.True(dequeueResult.Success);
        Assert.Null(found);
    }

    private async Task GivenAMoveInTransactionHasBeenAccepted()
    {
        var incomingMessage = new IncomingMessageBuilder()
            .WithMarketEvaluationPointId(SampleData.MeteringPointNumber)
            .WithProcessType(ProcessType.MoveIn.Code)
            .WithReceiver(SampleData.ReceiverId)
            .WithSenderId(SampleData.SenderId)
            .WithConsumerName(SampleData.ConsumerName)
            .WithEnergySupplierId(SampleData.NewEnergySupplierNumber)
            .WithMessageId(SampleData.OriginalMessageId)
            .WithTransactionId(SampleData.TransactionId)
            .Build();

        await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);
    }
}
