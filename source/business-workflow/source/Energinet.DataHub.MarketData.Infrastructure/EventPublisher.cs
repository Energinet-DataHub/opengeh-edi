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
using System.Text.Json;
using System.Threading.Tasks;
using Energinet.DataHub.MarketData.Application.Common.IntegrationEvents;
using Energinet.DataHub.MarketData.Infrastructure.IntegrationEvents;
using Energinet.DataHub.MarketData.Infrastructure.Outbox;
using NodaTime;

namespace Energinet.DataHub.MarketData.Infrastructure
{
    public class EventPublisher : IEventPublisher
    {
        private readonly IOutgoingActorMessage _outgoingActorMessage;

        public EventPublisher()
        {
            _outgoingActorMessage = new OutgoingActorMessageStub();
        }

        public IOutgoingActorMessage OutgoingActorMessage => _outgoingActorMessage;

        public Task PublishAsync<TIntegrationEvent>(TIntegrationEvent integrationEvent)
            where TIntegrationEvent : IIntegrationEvent
        {
            if (integrationEvent is null)
            {
                throw new ArgumentNullException(nameof(integrationEvent));
            }

            var type = integrationEvent.GetType().FullName;
            var data = JsonSerializer.Serialize(integrationEvent);
            _outgoingActorMessage.Add(new OutgoingActorMessage(SystemClock.Instance.GetCurrentInstant(), type!, data));
            return Task.CompletedTask;
        }
    }
}
