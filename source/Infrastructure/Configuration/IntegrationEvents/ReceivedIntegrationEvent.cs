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

using NodaTime;

namespace Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents;

public class ReceivedIntegrationEvent
{
    public ReceivedIntegrationEvent(string id, string eventType, string eventPayload, Instant occurredOn)
    {
        Id = id;
        OccurredOn = occurredOn;
        EventType = eventType;
        EventPayload = eventPayload;
    }

    #pragma warning disable CS8618 // Needed by ORM
    private ReceivedIntegrationEvent()
    {
    }

    public string Id { get; }

    public Instant OccurredOn { get; }

    public string EventType { get; }

    public string EventPayload { get; }
}
