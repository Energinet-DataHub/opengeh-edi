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
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.EDI.ActorMessageQueue.Contracts;
using Energinet.DataHub.EDI.ActorMessageQueue.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Common;
using Energinet.DataHub.EDI.Common.Actors;
using Energinet.DataHub.EDI.Common.DataAccess;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages;

public class WhenADequeueIsRequestedTests : TestBase
{
    private readonly IEnqueueMessage _enqueueMessage;
    private readonly OutgoingMessageDtoBuilder _outgoingMessageDtoBuilder;

    public WhenADequeueIsRequestedTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _outgoingMessageDtoBuilder = new OutgoingMessageDtoBuilder();
        _enqueueMessage = GetService<IEnqueueMessage>();
    }

    [Fact]
    public async Task Dequeue_is_unsuccessful_when_bundle_does_not_exist()
    {
        var dequeueResult = await InvokeCommandAsync(new DequeueCommand(Guid.NewGuid().ToString(), MarketRole.EnergySupplier, ActorNumber.Create(SampleData.SenderId)));

        Assert.False(dequeueResult.Success);
    }

    [Fact]
    public async Task Dequeue_unknown_message_id_is_unsuccessful_when_actor_has_a_queue()
    {
        var unknownMessageId = Guid.NewGuid().ToString();
        var enqueueMessageEvent = _outgoingMessageDtoBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .WithReceiverRole(MarketRole.EnergySupplier)
            .Build();
        await EnqueueMessage(enqueueMessageEvent);
        var dequeueResult = await InvokeCommandAsync(new DequeueCommand(unknownMessageId, MarketRole.EnergySupplier, ActorNumber.Create(SampleData.SenderId)));

        Assert.False(dequeueResult.Success);
    }

    [Fact]
    public async Task Dequeue_is_Successful()
    {
        var enqueueMessageEvent = _outgoingMessageDtoBuilder
            .WithReceiverNumber(SampleData.NewEnergySupplierNumber)
            .WithReceiverRole(MarketRole.EnergySupplier)
            .Build();
        await EnqueueMessage(enqueueMessageEvent);
        var peekResult = await InvokeCommandAsync(new PeekCommand(
            ActorNumber.Create(SampleData.NewEnergySupplierNumber),
            MessageCategory.Aggregations,
            MarketRole.EnergySupplier,
            DocumentFormat.Xml));

        var dequeueResult = await InvokeCommandAsync(new DequeueCommand(peekResult.MessageId.GetValueOrDefault().ToString(), MarketRole.EnergySupplier, ActorNumber.Create(SampleData.SenderId)));

        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var found = await connection
            .QuerySingleOrDefaultAsync<bool>("SELECT IsDequeued FROM [dbo].Bundles");

        Assert.True(dequeueResult.Success);
        Assert.True(found);
    }

    private async Task EnqueueMessage(OutgoingMessageDto message)
    {
        await _enqueueMessage.EnqueueAsync(message);
        await GetService<ActorMessageQueueContext>().SaveChangesAsync();
    }
}
