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
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Process.Interfaces;
using NodaTime;

namespace Energinet.DataHub.EDI.Process.Infrastructure.InboxEvents;

public class InboxEventReceiver : IInboxEventReceiver
{
    private readonly ProcessContext _context;
    private readonly IClock _clock;
    private readonly IEnumerable<IInboxEventMapper> _mappers;

    public InboxEventReceiver(
        ProcessContext context,
        IClock clock,
        IEnumerable<IInboxEventMapper> mappers)
    {
        _context = context;
        _clock = clock;
        _mappers = mappers;
    }

    public async Task ReceiveAsync(EventId eventId, string eventType, Guid referenceId, byte[] eventPayload)
    {
        ArgumentNullException.ThrowIfNull(eventId);

        if (!EventIsKnown(eventType)) return;

        if (await EventIsAlreadyRegisteredAsync(eventId).ConfigureAwait(false) == false)
        {
            await RegisterAsync(eventId, eventType, referenceId, eventPayload).ConfigureAwait(false);
        }
    }

    private async Task<bool> EventIsAlreadyRegisteredAsync(EventId eventId)
    {
        var inboxMessage = await _context.ReceivedInboxEvents.FindAsync(eventId.Value).ConfigureAwait(false);
        return inboxMessage is not null;
    }

    private bool EventIsKnown(string eventType)
    {
        return _mappers.Any(handler => handler.CanHandle(eventType));
    }

    private async Task RegisterAsync(EventId eventId, string eventType, Guid referenceId, byte[] eventPayload)
    {
        _context.ReceivedInboxEvents.Add(new ReceivedInboxEvent(eventId.Value, eventType, referenceId, eventPayload, _clock.GetCurrentInstant()));
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }
}
