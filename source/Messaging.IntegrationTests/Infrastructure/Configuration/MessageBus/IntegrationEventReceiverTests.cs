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

using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Infrastructure.Configuration.DataAccess;
using Messaging.Infrastructure.Configuration.MessageBus;
using Messaging.IntegrationTests.Fixtures;
using Xunit;

namespace Messaging.IntegrationTests.Infrastructure.Configuration.MessageBus;

public class IntegrationEventReceiverTests : TestBase
{
    private readonly IntegrationEventReceiver _receiver;

    public IntegrationEventReceiverTests(DatabaseFixture databaseFixture)
     : base(databaseFixture)
    {
        _receiver = new IntegrationEventReceiver(GetService<B2BContext>());
    }

    [Fact]
    public async Task Event_is_registered()
    {
        var eventId = "1";

        await EventIsReceived(eventId);

        await EventIsRegisteredWithInbox(eventId);
    }

    [Fact]
    public async Task Event_registration_is_omitted_if_already_registered()
    {
        var eventId = "1";
        await EventIsReceived(eventId).ConfigureAwait(false);

        await EventIsReceived(eventId).ConfigureAwait(false);

        await EventIsRegisteredWithInbox(eventId);
    }

    private static byte[] CreateEventPayload(TestIntegrationEvent @event)
    {
        return JsonSerializer.SerializeToUtf8Bytes(@event);
    }

    private Task EventIsReceived(string eventId)
    {
        var eventType = "TestEvent";
        var @event = new TestIntegrationEvent();
        var eventPayload = CreateEventPayload(@event);

        return _receiver.ReceiveAsync(eventId, eventType, eventPayload);
    }

    private async Task EventIsRegisteredWithInbox(string eventId)
    {
        var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync().ConfigureAwait(false);
        var isRegistered = connection.ExecuteScalar<bool>($"SELECT COUNT(*) FROM b2b.InboxMessages WHERE Id = @EventId", new { EventId = eventId, });
        Assert.True(isRegistered);
    }
}
#pragma warning disable


public class TestIntegrationEvent
{
}
