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
/// Client for for interacting with outgoing messages.
/// </summary>
public interface IOutgoingMessagesClient
{
    /// <summary>
    /// Dequeues a message from the queue and commit.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    Task<DequeueRequestResultDto> DequeueAndCommitAsync(DequeueRequestDto request, CancellationToken cancellationToken);

    /// <summary>
    ///  Peek a message from the queue and commit.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    Task<PeekResultDto> PeekAndCommitAsync(PeekRequestDto request, CancellationToken cancellationToken);

    /// <summary>
    ///  Enqueue a message, no commit. Currently ONLY used by the Process module which handles the commit itself.
    /// </summary>
    /// <param name="outgoingMessage"></param>
    Task EnqueueAsync(OutgoingMessageDto outgoingMessage);

    /// <summary>
    ///     Enqueue a message, WITH commit. Currently ONLY used by the Process module wrt reception of events.
    /// </summary>
    Task EnqueueAndCommitAsync(OutgoingMessageDto outgoingMessage, CancellationToken cancellationToken);
}
