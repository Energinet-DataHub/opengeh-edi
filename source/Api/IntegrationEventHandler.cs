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
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.Core.Messaging.Communication.Subscriber;
using Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents;
using Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents.IntegrationEventProcessors;
using Google.Protobuf;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.Api;

#pragma warning disable CA1711
public class IntegrationEventHandler : IIntegrationEventHandler
#pragma warning restore CA1711
{
    private readonly ILogger<IntegrationEventHandler> _logger;
    private readonly IReceivedIntegrationEventRepository _receivedIntegrationEventRepository;
    private readonly ReadOnlyCollection<IIntegrationEventProcessor> _integrationEventProcessors;

    public IntegrationEventHandler(ILogger<IntegrationEventHandler> logger, IReceivedIntegrationEventRepository receivedIntegrationEventRepository, IEnumerable<IIntegrationEventProcessor> integrationEventProcessors)
    {
        _logger = logger;
        _receivedIntegrationEventRepository = receivedIntegrationEventRepository;
        _integrationEventProcessors = new ReadOnlyCollection<IIntegrationEventProcessor>(integrationEventProcessors.ToList());
    }

    public async Task HandleAsync(IntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var processor = _integrationEventProcessors.SingleOrDefault(i => i.CanHandle(integrationEvent.EventName));

        if (!ShouldHandleEvent(processor))
            return;

        var addResult = await _receivedIntegrationEventRepository.AddIfNotExistsAsync(integrationEvent.EventIdentification, integrationEvent.EventName).ConfigureAwait(false);

        if (addResult != AddReceivedIntegrationEventResult.EventRegistered)
        {
            _logger.LogWarning("Integration event \"{EventIdentification}\" with event type \"{EventType}\" wasn't registered successfully. Registration result: {RegisterIntegrationEventResult}", integrationEvent.EventIdentification, integrationEvent.EventName, addResult.ToString());
            return;
        }

        await processor.ProcessAsync(integrationEvent).ConfigureAwait(false);
    }

    private static bool ShouldHandleEvent([NotNullWhen(true)] IIntegrationEventProcessor? processor) => processor != null;
}
