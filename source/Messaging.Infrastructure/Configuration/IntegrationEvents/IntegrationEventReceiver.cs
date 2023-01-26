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

using System.Threading.Tasks;
using Messaging.Application.Configuration;
using Messaging.Infrastructure.Configuration.DataAccess;
using Messaging.Infrastructure.Configuration.Processing.Inbox;

namespace Messaging.Infrastructure.Configuration.IntegrationEvents;

public class IntegrationEventReceiver
{
    private readonly B2BContext _context;
    private readonly ISystemDateTimeProvider _dateTimeProvider;

    public IntegrationEventReceiver(B2BContext context, ISystemDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task ReceiveAsync(string eventId, string eventType, byte[] eventPayload)
    {
        var inboxMessage = await _context.InboxMessages.FindAsync(eventId).ConfigureAwait(false);
        if (inboxMessage is not null)
        {
            return;
        }

        await RegisterAsync(eventId, eventType, eventPayload).ConfigureAwait(false);
    }

    private async Task RegisterAsync(string eventId, string eventType, byte[] eventPayload)
    {
        _context.InboxMessages.Add(new InboxMessage(eventId, eventType, eventPayload, _dateTimeProvider.Now()));
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }
}
