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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.Core.Messaging.Communication;

namespace Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents.IntegrationEventProcessors;

/// <summary>
/// Process specific type(s) of integration events
/// </summary>
public interface IIntegrationEventProcessor
{
    /// <summary>
    /// Collection of event types the processor handles
    /// </summary>
    public ReadOnlyCollection<string> EventTypesToHandle { get; }

    /// <summary>
    /// Process a single integration event
    /// </summary>
    /// <param name="integrationEvent">The integration event to process</param>
    /// <returns>A async task</returns>
    public Task ProcessAsync(IntegrationEvent integrationEvent);

    /// <summary>
    /// Determines whether the specified event type can be handled by the processor
    /// </summary>
    /// <param name="eventType">The event type</param>
    /// <returns>Whether the processor can handle the supplied event type</returns>
    public bool CanHandle(string eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentNullException(nameof(eventType));

        return EventTypesToHandle.Any(e => e.Equals(eventType, StringComparison.OrdinalIgnoreCase));
    }
}
