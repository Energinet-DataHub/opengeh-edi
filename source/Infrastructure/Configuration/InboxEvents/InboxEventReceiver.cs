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
using System.Threading;
using System.Threading.Tasks;
using Application.Configuration.Commands.Commands;
using Application.IncomingMessages.RequestAggregatedMeasureData;
using Energinet.DataHub.Edi.Responses.AggregatedMeasureData;
using MediatR;

namespace Infrastructure.Configuration.InboxEvents;

public class InboxEventReceiver
{
    private readonly IReadOnlyList<IInboxEventMapper> _mappers;
    private readonly IMediator _mediator;

    public InboxEventReceiver(IReadOnlyList<IInboxEventMapper> mappers, IMediator mediator)
    {
        _mappers = mappers;
        _mediator = mediator;
    }

    public async Task ReceiveAsync(string eventId, string eventName, byte[] eventPayload)
    {
        if (!EventIsKnown(eventName)) return;

        if (await EventIsAlreadyRegisteredAsync(eventId).ConfigureAwait(false) == false)
        {
            await RegisterAsync(eventId, eventName, eventPayload).ConfigureAwait(false);
        }
    }

    //TODO: How do we want to handle this?
    private static async Task<bool> EventIsAlreadyRegisteredAsync(string processId)
    {
        return await Task.FromResult(false).ConfigureAwait(false);
    }

    private bool EventIsKnown(string eventType)
    {
        return _mappers.Any(handler => handler.CanHandle(eventType));
    }

    private async Task RegisterAsync(string eventId, string eventName, byte[] eventPayload)
    {
        // TODO: make this dynamic
        var command = Parse(eventId, eventName, eventPayload);
        await _mediator.Send(command, CancellationToken.None).ConfigureAwait(false);
    }

    private ICommand<Unit> Parse(string eventId, string eventName, byte[] eventPayload)
    {
        var mapper = _mappers.First(mapper => mapper.CanHandle(eventName));



        var aggregatedTimeSeries = AggregatedTimeSeriesRequestAccepted.Parser.ParseFrom(eventPayload);
        var command = new AggregatedMeasureDataAccepted(aggregatedTimeSeries, Guid.Parse(eventId));

        return command;
    }
}
