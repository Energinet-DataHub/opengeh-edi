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
    private readonly IntegrationEventRegistrar _integrationEventRegistrar;
    private readonly IntegrationEventProcessorFactory _integrationEventProcessorFactory;

    public IntegrationEventHandler(ILogger<IntegrationEventHandler> logger, IntegrationEventRegistrar integrationEventRegistrar, IntegrationEventProcessorFactory integrationEventProcessorFactory)
    {
        _logger = logger;
        _integrationEventRegistrar = integrationEventRegistrar;
        _integrationEventProcessorFactory = integrationEventProcessorFactory;
    }

    public async Task HandleAsync(IntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var registerResult = await _integrationEventRegistrar.RegisterAsync(integrationEvent.EventIdentification.ToString(), integrationEvent.EventName).ConfigureAwait(false);

        if (registerResult != RegisterIntegrationEventResult.EventRegistered)
        {
            _logger.LogWarning("Integration event \"{EventIdentification}\" with event type \"{EventType}\" wasn't registered successfully. Registration result: {RegisterIntegrationEventResult}", integrationEvent.EventIdentification, integrationEvent.EventName, registerResult.ToString());
            return;
        }

        var processor = _integrationEventProcessorFactory.Get(integrationEvent.EventName);
        await processor.ProcessAsync(integrationEvent).ConfigureAwait(false);
    }
}
