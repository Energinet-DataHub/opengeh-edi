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

namespace Messaging.Domain.OutgoingMessages;

/// <summary>
/// Writes outgoing messages
/// </summary>
public interface IMessageWriter
{
    /// <summary>
    /// Determine if specified format can be handled by message writer
    /// </summary>
    /// <param name="format"></param>
    bool HandlesFormat(MessageFormat format);

    /// <summary>
    /// Determine if specified message type can be handles by the writer
    /// </summary>
    /// <param name="messageType"></param>
    bool HandlesType(MessageType messageType);

    /// <summary>
    /// Writes the message
    /// </summary>
    /// <param name="header"></param>
    /// <param name="marketActivityRecords"></param>
    Task<Stream> WriteAsync(MessageHeader header, IReadOnlyCollection<string> marketActivityRecords);
}
