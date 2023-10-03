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
using Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents.IntegrationEventProcessors;
using Energinet.DataHub.EDI.IntegrationTests.Fixtures;
using MediatR;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Energinet.DataHub.EDI.IntegrationTests.Infrastructure.Configuration.IntegrationEvents;

public class WhenAnIntegrationEventIsReceivedTests : TestBase
{
    private readonly string _testIntegrationEventType = nameof(TestIntegrationEvent);
    private readonly string _eventId1 = "1";

    private readonly IntegrationEventRegistrar _registrar;

    public WhenAnIntegrationEventIsReceivedTests(DatabaseFixture databaseFixture)
     : base(databaseFixture)
    {
        _registrar = new IntegrationEventRegistrar(GetService<B2BContext>(), GetService<ISystemDateTimeProvider>(), new IIntegrationEventProcessor[]
        {
            new TestIntegrationEventProcessor(),
        });
    }

    [Fact]
    public async Task Event_is_registered_if_it_is_a_known_event()
    {
        var registeredResult = await RegisterEvent(_eventId1, _testIntegrationEventType).ConfigureAwait(false);

        var isRegistered = await EventIsRegisteredInDatabase(_eventId1).ConfigureAwait(false);

        Assert.Equal(RegisterIntegrationEventResult.EventRegistered, registeredResult);
        Assert.True(isRegistered);
    }

    [Fact]
    public async Task Event_is_not_registered_if_it_is_unknown()
    {
        var unknownEventTypeResult = await RegisterEvent(_eventId1, "unknown-event-type").ConfigureAwait(false);

        var isRegistered = await EventIsRegisteredInDatabase(_eventId1).ConfigureAwait(false);

        Assert.Equal(RegisterIntegrationEventResult.EventTypeIsUnknown, unknownEventTypeResult);
        Assert.False(isRegistered);
    }

    [Fact]
    public async Task Event_registration_is_omitted_if_already_registered()
    {
        var firstRegisterResult = await RegisterEvent(_eventId1, _testIntegrationEventType).ConfigureAwait(false);

        var secondRegisterResult = await RegisterEvent(_eventId1, _testIntegrationEventType).ConfigureAwait(false);

        var isRegistered = await EventIsRegisteredInDatabase(_eventId1).ConfigureAwait(false);

        Assert.Equal(RegisterIntegrationEventResult.EventRegistered, firstRegisterResult);
        Assert.Equal(RegisterIntegrationEventResult.EventIsAlreadyRegistered, secondRegisterResult);
        Assert.True(isRegistered);
    }

    private static byte[] CreateEventPayload(TestIntegrationEvent @event)
    {
        return JsonSerializer.SerializeToUtf8Bytes(@event);
    }

    private Task<RegisterIntegrationEventResult> RegisterEvent(string eventId, string eventType)
    {
        return _registrar.RegisterAsync(eventId, eventType);
    }

    private async Task<bool> EventIsRegisteredInDatabase(string eventId)
    {
        var connection = await GetService<IDatabaseConnectionFactory>().GetConnectionAndOpenAsync(CancellationToken.None).ConfigureAwait(false);
        var isRegistered = connection.ExecuteScalar<bool>($"SELECT COUNT(*) FROM dbo.ReceivedIntegrationEvents WHERE Id = @EventId", new { EventId = eventId, });
        return isRegistered;
    }
}
