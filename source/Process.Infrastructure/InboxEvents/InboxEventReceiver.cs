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
using Energinet.DataHub.EDI.Common.DateTime;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;

namespace Energinet.DataHub.EDI.Process.Infrastructure.InboxEvents;

public class InboxEventReceiver
{
    private readonly ProcessContext _context;
    private readonly ISystemDateTimeProvider _dateTimeProvider;
    private readonly IEnumerable<IInboxEventMapper> _mappers;

    public InboxEventReceiver(
        ProcessContext context,
        ISystemDateTimeProvider dateTimeProvider,
        IEnumerable<IInboxEventMapper> mappers)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _mappers = mappers;
    }

    public async Task ReceiveAsync(string eventId, string eventType, Guid referenceId, byte[] eventPayload)
    {
        if (!EventIsKnown(eventType)) return;

        if (await EventIsAlreadyRegisteredAsync(eventId).ConfigureAwait(false) == false)
        {
            await RegisterAsync(eventId, eventType, referenceId, eventPayload).ConfigureAwait(false);
        }
    }

    private async Task<bool> EventIsAlreadyRegisteredAsync(string eventId)
    {
        var inboxMessage = await _context.ReceivedInboxEvents.FindAsync(eventId).ConfigureAwait(false);
        return inboxMessage is not null;
    }

    private bool EventIsKnown(string eventType)
    {
        return _mappers.Any(handler => handler.CanHandle(eventType));
    }

    private async Task RegisterAsync(string eventId, string eventType, Guid referenceId, byte[] eventPayload)
    {
        _context.ReceivedInboxEvents.Add(new ReceivedInboxEvent(eventId, eventType, referenceId, eventPayload, _dateTimeProvider.Now()));
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }
}
