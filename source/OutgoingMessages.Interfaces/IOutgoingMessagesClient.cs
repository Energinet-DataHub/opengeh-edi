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

using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;

namespace Energinet.DataHub.EDI.OutgoingMessages.Interfaces;

/// <summary>
/// Client for interacting with outgoing messages.
/// </summary>
public interface IOutgoingMessagesClient
{
    /// <summary>
    ///  Dequeues a message from the queue and commit.
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
    ///  Enqueue a accepted energy result message, no commit. Currently ONLY used by the Process module which handles the commit itself.
    /// </summary>
    Task<OutgoingMessageId> EnqueueAsync(AcceptedEnergyResultMessageDto acceptedEnergyResultMessage, CancellationToken cancellationToken);

    /// <summary>
    ///  Enqueue a rejected energy result message, no commit. Currently ONLY used by the Process module which handles the commit itself.
    /// </summary>
    Task<OutgoingMessageId> EnqueueAsync(RejectedEnergyResultMessageDto rejectedEnergyResultMessage, CancellationToken cancellationToken);

    /// <summary>
    ///     Enqueue a rejected wholesale service message, no commit. Currently ONLY used by the Process module which handles the
    ///     commit itself.
    /// </summary>
    Task<OutgoingMessageId> EnqueueAsync(
        RejectedWholesaleServicesMessageDto rejectedWholesaleServicesMessage,
        CancellationToken cancellationToken);

    /// <summary>
    ///  Enqueue a energy result message, WITH commit. Currently ONLY used by the integration event.
    /// </summary>
    Task<OutgoingMessageId> EnqueueAndCommitAsync(EnergyResultMessageDto energyResultMessage, CancellationToken cancellationToken);

    /// <summary>
    ///  Enqueue wholesale messages, handles enqueuing messages to all appropriate parties (Receiver, ChargeOwner) in a single transaction.
    /// </summary>
    Task EnqueueAndCommitAsync(WholesaleServicesMessageDto wholesaleServicesMessage, CancellationToken cancellationToken);

    /// <summary>
    ///  Enqueue a accepted wholesale services message, no commit. Currently ONLY used by the Process module which handles the commit itself.
    /// </summary>
    Task<OutgoingMessageId> EnqueueAsync(AcceptedWholesaleServicesMessageDto acceptedWholesaleServicesMessage, CancellationToken cancellationToken);

    /// <summary>
    ///  Enqueue a wholesale services total sum message, WITH commit. Currently ONLY used by the integration event.
    /// </summary>
    Task<OutgoingMessageId> EnqueueAndCommitAsync(WholesaleServicesTotalSumMessageDto wholesaleServicesTotalSumMessage, CancellationToken cancellationToken);

    /// <summary>
    ///  Enqueue energy results as outgoing messages for the given calculation id.
    /// </summary>
    Task<int> EnqueueByCalculationIdAsync(EnqueueMessagesInputDto input);
}
