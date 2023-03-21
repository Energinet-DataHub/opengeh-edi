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
using Infrastructure.Configuration.IntegrationEvents;
using MediatR;

namespace IntegrationTests.Infrastructure.Configuration.IntegrationEvents;

public class TestIntegrationEventMapper : IIntegrationEventMapper
{
    #pragma warning disable // Method cannot be static since inherited from the interface
    public Task<INotification> MapFromAsync(string payload)
    {
        var integrationEvent = JsonSerializer.Deserialize<TestIntegrationEvent>(payload);
        return Task.FromResult((INotification)new TestNotification(integrationEvent!.Property1));
    }

    public bool CanHandle(string eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        return eventType.Equals(nameof(TestIntegrationEvent), StringComparison.OrdinalIgnoreCase);
    }

    public string ToJson(byte[] payload)
    {
        var integrationEvent = JsonSerializer.Deserialize<TestIntegrationEvent>(payload);
        return JsonSerializer.Serialize(integrationEvent);
    }
}
