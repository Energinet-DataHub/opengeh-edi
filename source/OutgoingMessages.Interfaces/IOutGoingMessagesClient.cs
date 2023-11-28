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

using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;

namespace Energinet.DataHub.EDI.OutgoingMessages.Interfaces;

/// <summary>
///   Contract for dequeuing messages
/// </summary>
public interface IOutGoingMessagesClient
{
    /// <summary>
    /// Dequeues a message from the queue
    /// </summary>
    /// <param name="request"></param>
    Task<DequeueRequestResult> DequeueAsync(DequeueRequestDto request);

    /// <summary>
    ///  Peek a message from the queue
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    Task<PeekResult> PeekAsync(PeekRequest request, CancellationToken cancellationToken);

    /// <summary>
    ///  Enqueue a message
    /// </summary>
    /// <param name="outgoingMessage"></param>
    Task EnqueueAsync(OutgoingMessageDto outgoingMessage);

    /// <summary>
    ///  Enqueue a message, commit the imdediately
    /// </summary>
    /// <param name="outgoingMessage"></param>
    Task EnqueueAndCommitAsync(OutgoingMessageDto outgoingMessage);
}
