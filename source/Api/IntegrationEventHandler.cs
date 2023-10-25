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
using Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents.IntegrationEventMappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.Api;

#pragma warning disable CA1711
public class IntegrationEventHandler : IIntegrationEventHandler
#pragma warning restore CA1711
{
    private readonly ILogger<IntegrationEventHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IReceivedIntegrationEventRepository _receivedIntegrationEventRepository;
    private readonly IReadOnlyDictionary<string, IIntegrationEventMapper> _integrationEventMappers;

    public IntegrationEventHandler(ILogger<IntegrationEventHandler> logger, IMediator mediator, IReceivedIntegrationEventRepository receivedIntegrationEventRepository, IReadOnlyDictionary<string, IIntegrationEventMapper> integrationEventMappers)
    {
        _logger = logger;
        _mediator = mediator;
        _receivedIntegrationEventRepository = receivedIntegrationEventRepository;
        _integrationEventMappers = integrationEventMappers;
    }

    public async Task HandleAsync(IntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var shouldHandleEvent = _integrationEventMappers.TryGetValue(integrationEvent.EventName, out var integrationEventMapper);

        if (!shouldHandleEvent)
            return;

        var addResult = await _receivedIntegrationEventRepository.AddIfNotExistsAsync(integrationEvent.EventIdentification, integrationEvent.EventName).ConfigureAwait(false);

        if (addResult != AddReceivedIntegrationEventResult.EventRegistered)
        {
            _logger.LogWarning("Integration event \"{EventIdentification}\" with event type \"{EventType}\" wasn't registered successfully. Registration result: {RegisterIntegrationEventResult}", integrationEvent.EventIdentification, integrationEvent.EventName, addResult.ToString());
            return;
        }

        var internalCommand = integrationEventMapper!.MapToCommand(integrationEvent);

        await _mediator.Send(internalCommand).ConfigureAwait(false);
    }
}
