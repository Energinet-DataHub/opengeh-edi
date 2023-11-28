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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Actors;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Application.OutgoingMessages;

public class WhenADequeueIsRequestedTests : TestBase
{
    private readonly OutgoingMessageDtoBuilder _outgoingMessageDtoBuilder;
    private readonly IOutGoingMessagesClient _outgoingMessagesClient;

    public WhenADequeueIsRequestedTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _outgoingMessageDtoBuilder = new OutgoingMessageDtoBuilder();
        _outgoingMessagesClient = GetService<IOutGoingMessagesClient>();
    }

    [Fact]
    public async Task Dequeue_is_unsuccessful_when_bundle_does_not_exist()
    {
        var dequeueResult = await _outgoingMessagesClient.DequeueAndCommitAsync(new DequeueRequestDto(Guid.NewGuid().ToString(), MarketRole.EnergySupplier, ActorNumber.Create(SampleData.SenderId)));

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
        var dequeueResult = await _outgoingMessagesClient.DequeueAndCommitAsync(new DequeueRequestDto(unknownMessageId, MarketRole.EnergySupplier, ActorNumber.Create(SampleData.SenderId)));

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
        var peekResult = await _outgoingMessagesClient.PeekAndCommitAsync(
            new PeekRequest(
            ActorNumber.Create(SampleData.NewEnergySupplierNumber),
            MessageCategory.Aggregations,
            MarketRole.EnergySupplier,
            DocumentFormat.Xml),
            CancellationToken.None);

        var dequeueResult = await _outgoingMessagesClient.DequeueAndCommitAsync(new DequeueRequestDto(peekResult.MessageId.GetValueOrDefault().ToString(), MarketRole.EnergySupplier, ActorNumber.Create(SampleData.SenderId)));
        await GetService<ActorMessageQueueContext>().SaveChangesAsync();
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var found = await connection
            .QuerySingleOrDefaultAsync<bool>("SELECT IsDequeued FROM [dbo].Bundles");

        Assert.True(dequeueResult.Success);
        Assert.True(found);
    }

    private async Task EnqueueMessage(OutgoingMessageDto message)
    {
        await _outgoingMessagesClient.EnqueueAsync(message);
        await GetService<ActorMessageQueueContext>().SaveChangesAsync();
    }
}
