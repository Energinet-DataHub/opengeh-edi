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
using Dapper;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Process.Infrastructure.InboxEvents;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.InboxEvents;

public class WhenAnInboxEventIsReceivedTests : TestBase
{
    private readonly string _eventType = nameof(TestInboxEvent);
    private readonly TestInboxEvent _event;
    private readonly byte[] _eventPayload;
    private readonly string _eventId = Guid.NewGuid().ToString();
    private readonly Guid _referenceId = Guid.NewGuid();
    private InboxEventReceiver _receiver;

    public WhenAnInboxEventIsReceivedTests(IntegrationTestFixture integrationTestFixture, ITestOutputHelper testOutputHelper)
        : base(integrationTestFixture, testOutputHelper)
    {
        _receiver = new InboxEventReceiver(
            GetService<ProcessContext>(),
            GetService<ISystemDateTimeProvider>(),
            new IInboxEventMapper[]
        {
            new TestInboxEventMapper(),
        });
        _event = new TestInboxEvent("event1");
        _eventPayload = CreateEventPayload(_event);
    }

    [Fact]
    public async Task Event_is_registered_if_it_is_a_known_event()
    {
        // Act
        await EventIsReceived(_eventId);

        // Assert
        await EventIsRegisteredWithInbox(_eventId);
    }

    [Fact]
    public async Task Event_is_not_registered_if_it_is_unknown()
    {
        // Arrange
        var noIntegrationEventMappers = new List<IInboxEventMapper>();
        _receiver = new InboxEventReceiver(
            GetService<ProcessContext>(),
            GetService<ISystemDateTimeProvider>(),
            noIntegrationEventMappers);

        // Act
        await EventIsReceived(_eventId);

        // Assert
        await EventIsRegisteredWithInbox(_eventId, 0);
    }

    [Fact]
    public async Task Event_registration_is_omitted_if_already_registered()
    {
        // Act
        await EventIsReceived(_eventId);

        await EventIsReceived(_eventId);

        // Assert
        await EventIsRegisteredWithInbox(_eventId, 1);
    }

    private static byte[] CreateEventPayload(TestInboxEvent @event)
    {
        return JsonSerializer.SerializeToUtf8Bytes(@event);
    }

    private Task EventIsReceived(string eventId)
    {
        return _receiver.ReceiveAsync(EventId.From(eventId), _eventType, _referenceId, _eventPayload);
    }

    private async Task EventIsRegisteredWithInbox(string eventId, int expectedNumberOfRegisteredEvents = 1)
    {
        using var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None);
        var numberOfRegisteredEvents = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM dbo.ReceivedInboxEvents WHERE Id = @EventId", new { EventId = eventId, });
        Assert.Equal(expectedNumberOfRegisteredEvents, numberOfRegisteredEvents);
    }
}
