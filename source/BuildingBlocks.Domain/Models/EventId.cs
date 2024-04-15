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
using System.Text.Json.Serialization;

namespace Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;

/// <summary>
/// An EventId symbolises a tracking id, typically received from a ServiceBusMessage's MessageId,
///     or in case of our IntegrationEvents, this is also the EventIdentifier
/// </summary>
[Serializable]
public record EventId
{
    [JsonConstructor]
    private EventId(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static EventId From(Guid eventIdentification) => new(eventIdentification.ToString());

    public static EventId From(string eventId) => new(eventId);
}
