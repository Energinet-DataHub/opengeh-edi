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

namespace Messaging.Application.OutgoingMessages.Common
{
    /// <summary>
    /// Service for parsing a message record to- and from a string payload
    /// </summary>
    public interface IMessageRecordParser
    {
        /// <summary>
        /// Parses a message record to a string
        /// </summary>
        /// <param name="messageRecord"></param>
        /// <returns><see cref="string"/></returns>
        string From<TMessageRecord>(TMessageRecord messageRecord);

        /// <summary>
        /// Parses a market activity record from a string payload
        /// </summary>
        /// <param name="payload"></param>
        TMessageRecord From<TMessageRecord>(string payload);
    }
}
