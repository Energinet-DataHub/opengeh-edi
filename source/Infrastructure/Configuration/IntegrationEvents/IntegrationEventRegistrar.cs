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
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Application.Configuration;
using Energinet.DataHub.EDI.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents.IntegrationEventProcessors;

namespace Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents;

public class IntegrationEventRegistrar
{
    private readonly B2BContext _context;
    private readonly ISystemDateTimeProvider _dateTimeProvider;
    private readonly IReadOnlyCollection<IIntegrationEventProcessor> _processors;

    public IntegrationEventRegistrar(B2BContext context, ISystemDateTimeProvider dateTimeProvider, IReadOnlyCollection<IIntegrationEventProcessor> processors)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _processors = processors;
    }

    public async Task<RegisterIntegrationEventResult> RegisterAsync(string eventId, string eventType)
    {
        if (!EventIsKnown(eventType)) return RegisterIntegrationEventResult.EventTypeIsUnknown;

        if (await EventIsAlreadyRegisteredAsync(eventId).ConfigureAwait(false))
            return RegisterIntegrationEventResult.EventIsAlreadyRegistered;

        _context.ReceivedIntegrationEvents.Add(new ReceivedIntegrationEvent(eventId, eventType, _dateTimeProvider.Now()));
        await _context.SaveChangesAsync().ConfigureAwait(false);

        return RegisterIntegrationEventResult.EventRegistered;
    }

    private async Task<bool> EventIsAlreadyRegisteredAsync(string eventId)
    {
        var integrationEvent = await _context.ReceivedIntegrationEvents.FindAsync(eventId).ConfigureAwait(false);
        return integrationEvent != null;
    }

    private bool EventIsKnown(string eventType)
    {
        return _processors.Any(mapper => mapper.CanHandle(eventType));
    }
}
