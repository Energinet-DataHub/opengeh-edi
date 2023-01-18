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
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Messaging.IntegrationTests.Infrastructure.Configuration.MessageBus;

public class IntegrationEventReceiverTests
{
    private readonly EventHandlerSpy _eventHandler;
    private readonly IntegrationEventReceiver _receiver;

    public IntegrationEventReceiverTests()
    {
        _eventHandler = new EventHandlerSpy();
        _receiver = new IntegrationEventReceiver(new List<IIntegrationEventHandler>() { _eventHandler });
    }

    [Fact]
    public async Task Throw_if_event_cannot_be_handled()
    {
        var eventId = "1";
        var eventType = "TestEvent";
        var @event = new TestIntegrationEvent();
        var eventPayload = CreateEventPayload(@event);

        await Assert.ThrowsAsync<UnknownIntegrationEventTypeException>(() => _receiver.ReceiveAsync(eventId, eventType, eventPayload))
            .ConfigureAwait(false);
    }

    [Fact]
    public async Task Event_is_handled()
    {
        _eventHandler.HandlesReturns = true;
        var eventId = "1";
        var eventType = "TestEvent";
        var @event = new TestIntegrationEvent();
        var eventPayload = CreateEventPayload(@event);

        await _receiver.ReceiveAsync(eventId, eventType, eventPayload).ConfigureAwait(false);

        _eventHandler.WasInvoked();
    }

    private static byte[] CreateEventPayload(TestIntegrationEvent @event)
    {
        return JsonSerializer.SerializeToUtf8Bytes(@event);
    }
}
#pragma warning disable



public class UnknownIntegrationEventTypeException : Exception
{
    public UnknownIntegrationEventTypeException(string message)
        : base(message)
    {
    }

    public UnknownIntegrationEventTypeException()
    {
    }

    public UnknownIntegrationEventTypeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public class TestIntegrationEvent
{
}

public class IntegrationEventReceiver
{
    private readonly List<IIntegrationEventHandler> _handlers;

    public IntegrationEventReceiver(IEnumerable<IIntegrationEventHandler> eventHandlers)
    {
        _handlers = eventHandlers.ToList();
    }

    public Task ReceiveAsync(string eventId, string eventType, byte[] eventPayload)
    {
        var handler = _handlers.FirstOrDefault(handler => handler.Handles(eventType));
        if (handler is null)
        {
            throw new UnknownIntegrationEventTypeException();
        }

        return handler.ProcessAsync(eventPayload);
    }
}

public interface IIntegrationEventHandler
{
    bool Handles(string eventType);
    Task ProcessAsync(byte[] eventPayload);
}

public class EventHandlerSpy : IIntegrationEventHandler
{
    private bool _wasProcessed;
    public bool HandlesReturns { get; set; }

    public void WasInvoked()
    {
        Assert.True(_wasProcessed);
    }

    public bool Handles(string eventType)
    {
        return HandlesReturns;
    }

    public Task ProcessAsync(byte[] eventPayload)
    {
        _wasProcessed = true;
        return Task.CompletedTask;
    }
}
