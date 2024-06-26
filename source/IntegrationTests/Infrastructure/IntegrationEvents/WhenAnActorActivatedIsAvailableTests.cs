﻿// Copyright 2020 Energinet DataHub A/S
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

using Dapper;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.Core.Messaging.Communication.Subscriber;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.IntegrationTests.Factories;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Contracts;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.IntegrationEvents;

public class WhenAnActorActivatedIsAvailableTests : TestBase
{
    private readonly IDatabaseConnectionFactory _connectionFactory;

    public WhenAnActorActivatedIsAvailableTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _connectionFactory = GetService<IDatabaseConnectionFactory>();
    }

    [Fact]
    public async Task New_actor_event_is_received_stores_actor()
    {
        var actorActivatedEvent = ActorActivatedEventBuilder.Build();

        await HavingReceivedAndHandledIntegrationEventAsync(ActorActivated.EventName, actorActivatedEvent);

        var actors = await GetActors(actorActivatedEvent.ActorNumber, actorActivatedEvent.ExternalActorId);
        Assert.Single(actors);
    }

    [Fact]
    public async Task New_actor_event_is_received_with_existing_actor_does_not_store_second_actor()
    {
        var actorActivatedEvent = ActorActivatedEventBuilder.Build();
        await HavingReceivedAndHandledIntegrationEventAsync(ActorActivated.EventName, actorActivatedEvent);

        await HavingReceivedAndHandledIntegrationEventAsync(ActorActivated.EventName, actorActivatedEvent);

        var actors = await GetActors(actorActivatedEvent.ActorNumber, actorActivatedEvent.ExternalActorId);
        Assert.Single(actors);
    }

    private async Task HavingReceivedAndHandledIntegrationEventAsync(string eventType, ActorActivated actorActivated)
    {
        var integrationEventHandler = GetService<IIntegrationEventHandler>();

        var integrationEvent = new IntegrationEvent(Guid.NewGuid(), eventType, 1, actorActivated);

        await integrationEventHandler.HandleAsync(integrationEvent).ConfigureAwait(false);
    }

    private async Task<IEnumerable<Actor>> GetActors(string actorNumber, string externalActorId)
    {
        using var connection = await _connectionFactory.GetConnectionAndOpenAsync(CancellationToken.None);
        var sql = $"SELECT Id, ActorNumber, ExternalId FROM [dbo].[Actor] WHERE ActorNumber = '{actorNumber}' AND ExternalId = '{externalActorId}'";
        return await connection.QueryAsync<Actor>(sql);
    }

#pragma warning disable
    public record Actor(Guid Id, string ActorNumber, string ExternalId);
#pragma warning restore
}
