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
using Messaging.Infrastructure.Configuration.Processing.Inbox;
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
        var eventType = "TestEvent";
        var @event = new TestIntegrationEvent();
        var eventPayload = CreateEventPayload(@event);

        await _receiver.ReceiveAsync(eventId, eventType, eventPayload).ConfigureAwait(false);

        var findRegisteredEventStatement = $"SELECT COUNT(*) FROM b2b.InboxMessages WHERE Id = @EventId";
        var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync().ConfigureAwait(false);
        var isRegistered = connection.ExecuteScalar<bool>(findRegisteredEventStatement, new { EventId = eventId, });
        Assert.True(isRegistered);
    }

    private static byte[] CreateEventPayload(TestIntegrationEvent @event)
    {
        return JsonSerializer.SerializeToUtf8Bytes(@event);
    }
}
#pragma warning disable


public class TestIntegrationEvent
{
}

public class IntegrationEventReceiver
{
    private readonly B2BContext _context;

    public IntegrationEventReceiver(B2BContext context)
    {
        _context = context;
    }

    public Task ReceiveAsync(string eventId, string eventType, byte[] eventPayload)
    {
        var inboxMessage = new InboxMessage(eventId);
        _context.InboxMessages.Add(inboxMessage);
        return _context.SaveChangesAsync();
    }
}
