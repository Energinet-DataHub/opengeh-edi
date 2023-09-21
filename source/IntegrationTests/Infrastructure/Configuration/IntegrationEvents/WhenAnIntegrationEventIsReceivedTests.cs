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
using Dapper;
using Energinet.DataHub.EDI.Application.Configuration;
using Energinet.DataHub.EDI.Application.Configuration.DataAccess;
using Energinet.DataHub.EDI.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Configuration.IntegrationEvents;

public class WhenAnIntegrationEventIsReceivedTests : TestBase
{
    private readonly string _eventType = nameof(TestIntegrationEvent);
    private readonly TestIntegrationEvent _event;
    private readonly byte[] _eventPayload;
    private readonly string _eventId = "1";
    private IntegrationEventReceiver _receiver;

    public WhenAnIntegrationEventIsReceivedTests(DatabaseFixture databaseFixture)
     : base(databaseFixture)
    {
        _receiver = new IntegrationEventReceiver(GetService<B2BContext>(), GetService<ISystemDateTimeProvider>(), new IIntegrationEventMapper[]
        {
            new TestIntegrationEventMapper(),
        });
        _event = new TestIntegrationEvent();
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
        var noIntegrationEventMappers = new List<IIntegrationEventMapper>();
        _receiver = new IntegrationEventReceiver(GetService<B2BContext>(), GetService<ISystemDateTimeProvider>(), noIntegrationEventMappers);

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

    [Fact]
    public async Task Event_is_marked_as_processed_when_a_handler_has_handled_it_successfully()
    {
        await EventIsReceived(_eventId).ConfigureAwait(false);

        await ProcessInboxMessages().ConfigureAwait(false);

        await EventIsMarkedAsProcessed(_eventId).ConfigureAwait(false);
    }

    [Fact]
    public async Task Event_is_marked_as_failed_if_the_event_handler_throws_an_exception()
    {
        ExceptEventHandlerToFail();
        await EventIsReceived(_eventId).ConfigureAwait(false);

        await ProcessInboxMessages().ConfigureAwait(false);

        await EventIsMarkedAsFailed(_eventId);
    }

    private static byte[] CreateEventPayload(TestIntegrationEvent @event)
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
        var isRegistered = connection.ExecuteScalar<bool>($"SELECT COUNT(*) FROM dbo.ReceivedIntegrationEvents WHERE Id = @EventId", new { EventId = eventId, });
        Assert.Equal(isExpectedToBeRegistered, isRegistered);
    }

    private async Task EventIsMarkedAsProcessed(string eventId)
    {
        var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None).ConfigureAwait(false);
        var isProcessed = connection.ExecuteScalar<bool>($"SELECT COUNT(*) FROM dbo.ReceivedIntegrationEvents WHERE Id = @EventId AND ProcessedDate IS NOT NULL", new { EventId = eventId, });
        Assert.True(isProcessed);
    }

    private async Task EventIsMarkedAsFailed(string eventId)
    {
        var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None).ConfigureAwait(false);
        var isFailed = connection.ExecuteScalar<bool>($"SELECT COUNT(*) FROM dbo.ReceivedIntegrationEvents WHERE Id = @EventId AND ProcessedDate IS NOT NULL AND ErrorMessage IS NOT NULL", new { EventId = eventId, });
        Assert.True(isFailed);
    }

    private Task ProcessInboxMessages()
    {
        var inboxProcessor = new IntegrationEventsProcessor(
            GetService<IDatabaseConnectionFactory>(),
            GetService<IMediator>(),
            GetService<ISystemDateTimeProvider>(),
            new[] { new TestIntegrationEventMapper(), },
            GetService<ILogger<IntegrationEventsProcessor>>());
        return inboxProcessor.ProcessMessagesAsync(CancellationToken.None);
    }

    private void ExceptEventHandlerToFail()
    {
        NotificationHandlerSpy.ShouldThrowException();
    }
}
