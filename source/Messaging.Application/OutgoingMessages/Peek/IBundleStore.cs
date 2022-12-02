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

using System.IO;
using System.Threading.Tasks;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages.Peek;

namespace Messaging.Application.OutgoingMessages.Peek;

/// <summary>
/// Interface to handle already peeked messages
/// </summary>
public interface IBundleStore
{
    /// <summary>
    /// Get already peeked document
    /// </summary>
    /// <param name="messageCategory">Type of messages within message bundle</param>
    /// <param name="messageReceiverNumber">Actor number of message receiver</param>
    /// <param name="roleOfReceiver">Market role of the receiver</param>
    /// <returns>A nullable stream containing peeked document</returns>
    Stream? GetBundleOf(MessageCategory messageCategory, ActorNumber messageReceiverNumber, MarketRole roleOfReceiver);

    /// <summary>
    /// Register peeked document
    /// </summary>
    /// <param name="key"></param>
    /// <param name="document"></param>
    void SetBundleFor(string key, Stream document);

    /// <summary>
    /// Register bundle key
    /// </summary>
    /// /// <param name="messageCategory">Type of messages within message bundle</param>
    /// <param name="messageReceiverNumber">Actor number of message receiver</param>
    /// <param name="roleOfReceiver">Market role of the receiver</param>
    Task<bool> TryRegisterBundleAsync(MessageCategory messageCategory, ActorNumber messageReceiverNumber, MarketRole roleOfReceiver);
}
