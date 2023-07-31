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

using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.Configuration;
using Application.Configuration.DataAccess;
using Dapper;
using Infrastructure.Configuration.DataAccess;
using Infrastructure.InboxEvents;
using IntegrationTests.Fixtures;
using Xunit;

namespace IntegrationTests.Infrastructure.InboxEvents;

public class WhenAnInboxEventIsReceivedTests : TestBase
{
    private readonly string _eventType = nameof(TestInboxEvent);
    private readonly TestInboxEvent _event;
    private readonly byte[] _eventPayload;
    private readonly string _eventId = "1";
    private InboxEventReceiver _receiver; // TODO: Change

    public WhenAnInboxEventIsReceivedTests(DatabaseFixture databaseFixture)
     : base(databaseFixture)
    {
        _receiver = new InboxEventReceiver(
            GetService<B2BContext>(),
            GetService<ISystemDateTimeProvider>(),
            new IInboxEventMapper[]
        {
            new TestInboxEventMapper(),
        });
        _event = new TestInboxEvent();
        _eventPayload = CreateEventPayload(_event);
    }

    [Fact]
    public async Task Event_is_registered_if_it_is_a_known_event()
    {
        await EventIsReceived(_eventId);

        await EventIsRegisteredWithInbox(_eventId);
    }

    [Fact]
    public async Task Event_is_not_registered_if_it_is_unknown()
    {
        var noIntegrationEventMappers = new List<IInboxEventMapper>();
        _receiver = new InboxEventReceiver(GetService<B2BContext>(), GetService<ISystemDateTimeProvider>(), noIntegrationEventMappers);

        await EventIsReceived(_eventId);

        await EventIsRegisteredWithInbox(_eventId, false);
    }

    [Fact]
    public async Task Event_registration_is_omitted_if_already_registered()
    {
        await EventIsReceived(_eventId).ConfigureAwait(false);

        await EventIsReceived(_eventId).ConfigureAwait(false);

        await EventIsRegisteredWithInbox(_eventId);
    }

    private static byte[] CreateEventPayload(TestInboxEvent @event)
    {
        return JsonSerializer.SerializeToUtf8Bytes(@event);
    }

    private Task EventIsReceived(string eventId)
    {
        return _receiver.ReceiveAsync(eventId, _eventType, _eventPayload);
    }

    private async Task EventIsRegisteredWithInbox(string eventId, bool isExpectedToBeRegistered = true)
    {
        var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None).ConfigureAwait(false);
        var isRegistered = connection.ExecuteScalar<bool>($"SELECT COUNT(*) FROM dbo.ReceivedInboxEvents WHERE Id = @EventId", new { EventId = eventId, });
        Assert.Equal(isExpectedToBeRegistered, isRegistered);
    }
}
