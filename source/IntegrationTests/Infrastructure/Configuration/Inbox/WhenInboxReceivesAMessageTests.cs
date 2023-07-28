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
using System.Threading;
using System.Threading.Tasks;
using Application.Configuration.DataAccess;
using Dapper;
using Infrastructure.Configuration.InboxEvents;
using IntegrationTests.Fixtures;
using MediatR;
using Xunit;

namespace IntegrationTests.Infrastructure.Configuration.Inbox;

public class WhenInboxReceivesAMessageTests : TestBase
{
    private readonly string _eventType = nameof(TestInboxEvent);
    private readonly TestInboxEvent _event;
    private readonly byte[] _eventPayload;
    private readonly string _eventId = "1";
    private readonly InboxEventReceiver _receiver;

    public WhenInboxReceivesAMessageTests(DatabaseFixture databaseFixture)
        : base(databaseFixture)
    {
        _receiver = new InboxEventReceiver(
            new IInboxEventMapper[]
            {
                new TestInboxEventMapper(),
#pragma warning disable SA1117
            }, GetService<IMediator>());
#pragma warning restore SA1117
        _event = new TestInboxEvent();
        _eventPayload = CreateEventPayload(_event);
    }

    [Fact]
    public async Task Event_is_made_to_internal_command_if_it_is_a_know_event()
    {
        await Assert.ThrowsAsync<TestSuccessException>(() => EventIsReceived(_eventId)).ConfigureAwait(false);
    }

    private static byte[] CreateEventPayload(TestInboxEvent @event)
    {
        return JsonSerializer.SerializeToUtf8Bytes(@event);
    }

    private Task EventIsReceived(string eventId)
    {
        return _receiver.ReceiveAsync(eventId, _eventType, _eventPayload);
    }
}
