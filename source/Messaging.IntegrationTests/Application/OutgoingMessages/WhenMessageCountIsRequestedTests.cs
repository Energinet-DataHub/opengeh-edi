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
using JetBrains.Annotations;
using Messaging.Application.OutgoingMessages.MessageCount;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.IntegrationTests.Application.IncomingMessages;
using Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace Messaging.IntegrationTests.Application.OutgoingMessages;

public class WhenMessageCountIsRequestedTests : TestBase
{
    public WhenMessageCountIsRequestedTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
    }

    [Fact]
    public async Task When_no_messages_are_available_return_zero()
    {
        var result = await InvokeCommandAsync(CreateMessageCountRequest(SampleData.NewEnergySupplierNumber))
            .ConfigureAwait(false);

        Assert.Equal(0, result.MessageCount);
    }

    [Fact]
    public async Task When_messages_are_available_return_count()
    {
        await GivenAMoveInTransactionHasBeenAccepted().ConfigureAwait(false);

        var result = await InvokeCommandAsync(CreateMessageCountRequest(SampleData.NewEnergySupplierNumber))
            .ConfigureAwait(false);

        Assert.Equal(1, result.MessageCount);
    }

    private static MessageCountRequest CreateMessageCountRequest(string actorNumber)
    {
        return new MessageCountRequest(ActorNumber.Create(SampleData.NewEnergySupplierNumber));
    }

    private static IncomingMessageBuilder MessageBuilder()
    {
        return new IncomingMessageBuilder()
            .WithEnergySupplierId(SampleData.NewEnergySupplierNumber)
            .WithMessageId(SampleData.OriginalMessageId)
            .WithTransactionId(SampleData.TransactionId);
    }

    private async Task GivenAMoveInTransactionHasBeenAccepted()
    {
        var incomingMessage = MessageBuilder()
            .WithMarketEvaluationPointId(SampleData.MeteringPointNumber)
            .WithProcessType(ProcessType.MoveIn.Code)
            .WithReceiver(SampleData.ReceiverId)
            .WithSenderId(SampleData.SenderId)
            .WithConsumerName(SampleData.ConsumerName)
            .Build();

        await InvokeCommandAsync(incomingMessage).ConfigureAwait(false);
    }
}
