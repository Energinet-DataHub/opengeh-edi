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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.SubsystemTests.Drivers.MessageFactories;
using NodaTime;

namespace Energinet.DataHub.EDI.SubsystemTests.Drivers;

internal class ProcessManagerDriver(
    ServiceBusSenderClient client)
{
    private readonly ServiceBusSenderClient _client = client;

    internal async Task PublishAcceptedBrs026RequestAsync(string gridArea, Actor actor)
    {
        var message = EnqueueBrs026MessageFactory.CreateAccept(actor, gridArea);
        await _client.SendAsync(message, CancellationToken.None);
    }

    internal async Task PublishRejectedBrs026RequestAsync(Actor actor)
    {
        var message = EnqueueBrs026MessageFactory.CreateReject(actor);
        await _client.SendAsync(message, CancellationToken.None);
    }

    internal async Task PublishAcceptedBrs028RequestAsync(string gridArea, Actor actor)
    {
        var message = EnqueueBrs028MessageFactory.CreateAccept(actor, gridArea);
        await _client.SendAsync(message, CancellationToken.None);
    }

    internal async Task PublishRejectedBrs028RequestAsync(Actor actor)
    {
        var message = EnqueueBrs028MessageFactory.CreateReject(actor);
        await _client.SendAsync(message, CancellationToken.None);
    }

    internal async Task PublishEnqueueBrs023_027RequestAsync(Guid calculationId)
    {
        var message = EnqueueBrs023_027MessageFactory.CreateEnqueue(calculationId);
        await _client.SendAsync(message, CancellationToken.None);
    }

    internal async Task PublishEnqueueBrs021AcceptedForwardMeteredDataAsync(
        List<(
            Actor Actor,
            Instant Start,
            Instant End,
            string OriginalActorMessageId,
            Guid EventId)> messages)
    {
        var serviceBusMessages = messages
            .Select(m => EnqueueBrs021ForwardMeteredDataFactory.CreateAcceptedV1(
                m.Actor,
                m.Start,
                m.End,
                m.OriginalActorMessageId,
                m.EventId))
            .ToList();
        await _client.SendAsync(serviceBusMessages, CancellationToken.None);
    }

    internal async Task PublishEnqueueBrs021AcceptedForwardMeteredDataAsync(
        Actor actor,
        Instant start,
        Instant end,
        string originalActorMessageId,
        Guid eventId,
        MeteringPointId? meteringPointId = null)
    {
        var serviceBusMessage = EnqueueBrs021ForwardMeteredDataFactory.CreateAcceptedV1(
            actor,
            start,
            end,
            originalActorMessageId,
            eventId,
            meteringPointId);

        await _client.SendAsync(serviceBusMessage, CancellationToken.None);
    }

    internal async Task PublishBrs021ForwardMeteredDataRejectedAsync(
        Actor actor,
        string originalActorMessageId,
        Guid eventId,
        string validationError)
    {
        var serviceBusMessage = EnqueueBrs021ForwardMeteredDataFactory.CreateRejectedV1(
            actor,
            originalActorMessageId,
            eventId,
            validationError);

        await _client.SendAsync(serviceBusMessage, CancellationToken.None);
    }
}
