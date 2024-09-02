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

namespace Energinet.DataHub.EDI.Outbox.Interfaces;

/// <summary>
/// An outbox message to be persisted in the outbox storage.
/// </summary>
public interface IOutboxMessage<out TPayload>
{
    /// <summary>
    /// The type of the message, used to categorize the message, and find the correct message processor
    /// </summary>
    string Type { get; }

    /// <summary>
    /// The payload of the outbox message
    /// </summary>
    TPayload Payload { get; }

    /// <summary>
    /// Serialized the outbox message as a string, typically using JSON serialization
    /// </summary>
    Task<string> SerializeAsync();
}
