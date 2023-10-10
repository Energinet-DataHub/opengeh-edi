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
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.EDI.Application.Configuration.Commands.Commands;

namespace Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents.IntegrationEventMappers;

/// <summary>
/// Process specific type(s) of integration events
/// </summary>
public interface IIntegrationEventMapper
{
    /// <summary>
    /// Event type the processor handles
    /// </summary>
    public string EventTypeToHandle { get; }

    /// <summary>
    /// Process a single integration event
    /// </summary>
    public InternalCommand MapToCommand(IntegrationEvent integrationEvent);

    /// <summary>
    /// Determines whether the specified event type can be handled by the processor
    /// </summary>
    /// <param name="eventType">The event type, something like IntegrationEvent.EventName</param>
    /// <returns>Whether the processor can handle the supplied event type</returns>
    public bool CanHandle(string eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentNullException(nameof(eventType));

        return EventTypeToHandle.Equals(eventType, StringComparison.OrdinalIgnoreCase);
    }
}
