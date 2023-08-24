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

using Energinet.DataHub.Core.Messaging.Communication.Subscriber;

namespace Energinet.DataHub.Core.Messaging.Communication.Internal.Subscriber;

internal sealed class Subscriber : ISubscriber
{
    private readonly IIntegrationEventFactory _integrationEventFactory;
    private readonly IIntegrationEventHandler _integrationEventHandler;

    public Subscriber(IIntegrationEventFactory integrationEventFactory, IIntegrationEventHandler integrationEventHandler)
    {
        _integrationEventFactory = integrationEventFactory;
        _integrationEventHandler = integrationEventHandler;
    }

    public async Task HandleAsync(IntegrationEventServiceBusMessage message)
    {
        if (_integrationEventFactory.TryCreate(message, out var integrationEvent))
        {
            await _integrationEventHandler.HandleAsync(integrationEvent).ConfigureAwait(false);
        }
    }
}
