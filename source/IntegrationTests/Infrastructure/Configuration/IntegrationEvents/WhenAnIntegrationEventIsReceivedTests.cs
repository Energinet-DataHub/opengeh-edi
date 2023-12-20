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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.EDI.Api;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents;
using Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents.IntegrationEventMappers;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.Logging;
using Xunit;
using IIntegrationEventHandler = Energinet.DataHub.Core.Messaging.Communication.Subscriber.IIntegrationEventHandler;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Configuration.IntegrationEvents;

public class WhenAnIntegrationEventIsReceivedTests : TestBase
{
    private readonly IntegrationEvent _testIntegrationEvent1;
    private readonly IntegrationEvent _unknownIntegrationEvent;

    private readonly TestIntegrationEventProcessor _testIntegrationEventProcessor;
    private readonly IIntegrationEventHandler _handler;

    public WhenAnIntegrationEventIsReceivedTests(DatabaseFixture databaseFixture)
     : base(databaseFixture)
    {
        _testIntegrationEventProcessor = new TestIntegrationEventProcessor(GetService<IMediator>());
        _handler = new IntegrationEventHandler(
                GetService<ILogger<IntegrationEventHandler>>(),
                GetService<IReceivedIntegrationEventRepository>(),
                new Dictionary<string, IIntegrationEventProcessor>
                {
                    { _testIntegrationEventProcessor.EventTypeToHandle, _testIntegrationEventProcessor },
                });

        _testIntegrationEvent1 = new IntegrationEvent(Guid.NewGuid(), TestIntegrationEventMessage.TestIntegrationEventName, 1, new TestIntegrationEventMessage());
        _unknownIntegrationEvent = new IntegrationEvent(Guid.NewGuid(), "unknown-event-type", 1, null!);
    }

    private string EventId1 => _testIntegrationEvent1.EventIdentification.ToString();

    private string EventIdUnknown => _unknownIntegrationEvent.EventIdentification.ToString();

    [Fact]
    public async Task Event_is_registered_and_mapped_if_it_is_a_known_event()
    {
        await HandleEvent(_testIntegrationEvent1);

        var isRegistered = await EventIsRegisteredInDatabase(EventId1);
        Assert.True(isRegistered);
        Assert.Equal(1, _testIntegrationEventProcessor.MappedCount);
    }

    [Fact]
    public async Task Event_is_not_registered_and_mapped_if_it_is_unknown()
    {
        await HandleEvent(_unknownIntegrationEvent);

        var isRegistered = await EventIsRegisteredInDatabase(EventIdUnknown);

        Assert.False(isRegistered);
        Assert.Equal(0, _testIntegrationEventProcessor.MappedCount);
    }

    [Fact]
    public async Task Event_registration_is_omitted_if_already_registered()
    {
        await HandleEvent(_testIntegrationEvent1);
        await HandleEvent(_testIntegrationEvent1);

        var isRegistered = await EventIsRegisteredInDatabase(EventId1);

        Assert.True(isRegistered);
        Assert.Equal(1, _testIntegrationEventProcessor.MappedCount);
    }

    [Fact]
    public async Task Event_registration_is_omitted_if_run_in_parallel()
    {
        var tasks = new List<Task>();
        Parallel.For(0, 10, (i) =>
        {
            var task = HandleEvent(_testIntegrationEvent1);
            tasks.Add(task);
        });

        await Task.WhenAll(tasks);

        var isRegistered = await EventIsRegisteredInDatabase(EventId1);

        Assert.True(isRegistered);
        Assert.Equal(1, _testIntegrationEventProcessor.MappedCount);
    }

    private Task HandleEvent(IntegrationEvent integrationEvent)
    {
        return _handler.HandleAsync(integrationEvent);
    }

    private async Task<bool> EventIsRegisteredInDatabase(string eventId)
    {
        var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var isRegistered = connection.ExecuteScalar<bool>($"SELECT COUNT(*) FROM dbo.ReceivedIntegrationEvents WHERE Id = @EventId", new { EventId = eventId, });
        return isRegistered;
    }
}
