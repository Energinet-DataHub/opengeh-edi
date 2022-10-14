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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Messaging.Domain.OutgoingMessages;

namespace Messaging.Application.OutgoingMessages
{
    /// <summary>
    /// Store for outgoing actor messages
    /// </summary>
    public interface IOutgoingMessageStore
    {
        /// <summary>
        /// Add message to queue
        /// </summary>
        /// <param name="message"></param>
        void Add(OutgoingMessage message);

        /// <summary>
        /// Get unpublished messages
        /// </summary>
        /// <returns> A read only collection of unpublished messages</returns>
        ReadOnlyCollection<OutgoingMessage> GetUnpublished();

        /// <summary>
        /// Gets an outgoing message from message store
        /// </summary>
        /// <param name="messageId"></param>
        /// <returns><see cref="OutgoingMessage"/></returns>
        OutgoingMessage? GetById(Guid messageId);

        /// <summary>
        /// Get outgoing message by the id of the transaction that generated the message
        /// </summary>
        /// <param name="transactionId"></param>
        /// <returns><see cref="OutgoingMessage"/></returns>
        OutgoingMessage? GetByTransactionId(string transactionId);

        /// <summary>
        /// Get outgoing messages by list of incoming message ids
        /// </summary>
        /// <param name="messageIds"></param>
        /// <returns><see cref="ReadOnlyCollection{OutgoingMessage}"/></returns>
        ReadOnlyCollection<OutgoingMessage> GetByIds(IReadOnlyCollection<string> messageIds);
    }
}
