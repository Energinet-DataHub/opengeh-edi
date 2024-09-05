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
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Dequeue;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.EnergyResultMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.EnergyResultMessages.Request;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Peek;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.WholesaleResultMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.WholesaleResultMessages.Request;

namespace Energinet.DataHub.EDI.OutgoingMessages.Interfaces;

/// <summary>
/// Client for interacting with outgoing messages.
/// TODO: This should only contain methods for interacting with outgoing messages from the outside,
/// ie. enqueuing EnergyResultMessageDto or WholesaleServicesMessageDto should be removed, since they are called
/// from OutgoingMessages.Infrastructure after changing to databrick views.
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
    /// Peek a message from the queue and commit.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    Task<PeekResultDto?> PeekAndCommitAsync(PeekRequestDto request, CancellationToken cancellationToken);

    /// <summary>
    /// Enqueue a accepted energy result message, no commit. Currently ONLY used by the Process module which handles the commit itself.
    /// <returns>The Id for the created OutgoingMessage</returns>
    /// </summary>
    Task<Guid> EnqueueAsync(AcceptedEnergyResultMessageDto acceptedEnergyResultMessage, CancellationToken cancellationToken);

    /// <summary>
    /// Enqueue a rejected energy result message, no commit. Currently ONLY used by the Process module which handles the commit itself.
    /// <returns>The Id for the created OutgoingMessage</returns>
    /// </summary>
    Task<Guid> EnqueueAsync(RejectedEnergyResultMessageDto rejectedEnergyResultMessage, CancellationToken cancellationToken);

    /// <summary>
    /// Enqueue a rejected wholesale service message, no commit. Currently ONLY used by the Process module which handles the
    /// commit itself.
    /// <returns>The Id for the created OutgoingMessage</returns>
    /// </summary>
    Task<Guid> EnqueueAsync(
        RejectedWholesaleServicesMessageDto rejectedWholesaleServicesMessage,
        CancellationToken cancellationToken);

    /// <summary>
    /// Enqueue an energy result message for a metered data responsible in a grid area, WITH commit.
    /// <returns>The Id for the created OutgoingMessage</returns>
    /// </summary>
    Task<Guid> EnqueueAndCommitAsync(EnergyResultPerGridAreaMessageDto messageDto, CancellationToken cancellationToken);

    /// <summary>
    /// Enqueue an energy result message for a balance responsible in a grid area, WITH commit.
    /// /// <returns>The Id for the created OutgoingMessage</returns>
    /// </summary>
    Task<Guid> EnqueueAndCommitAsync(EnergyResultPerBalanceResponsibleMessageDto messageDto, CancellationToken cancellationToken);

    /// <summary>
    /// Enqueue energy result messages for an energy supplier and a balance responsible in a grid area, WITH commit.
    /// </summary>
    /// <returns>The a list containing the Id's for the created OutgoingMessages</returns>
    Task<IReadOnlyCollection<Guid>> EnqueueAndCommitAsync(EnergyResultPerEnergySupplierPerBalanceResponsibleMessageDto messageDto, CancellationToken cancellationToken);

    /// <summary>
    /// Enqueue wholesale total amount messages, handles enqueuing messages to receiver in a single transaction.
    /// </summary>
    Task<Guid> EnqueueAndCommitAsync(WholesaleTotalAmountMessageDto wholesaleTotalAmountMessageDto, CancellationToken cancellationToken);

    /// <summary>
    /// Enqueue wholesale monthly amount per charge messages, handles enqueuing messages to all appropriate parties (Receiver, ChargeOwner) in a single transaction.
    /// </summary>
    Task EnqueueAndCommitAsync(WholesaleMonthlyAmountPerChargeMessageDto wholesaleMonthlyAmountPerChargeMessageDto, CancellationToken cancellationToken);

    /// <summary>
    /// Enqueue amount per charge messages, handles enqueuing messages to all appropriate parties (Receiver, ChargeOwner) in a single transaction.
    /// </summary>
    Task EnqueueAndCommitAsync(WholesaleAmountPerChargeMessageDto wholesaleAmountPerChargeMessageDto, CancellationToken cancellationToken);

    /// <summary>
    /// Enqueue an accepted wholesale services message, no commit. Currently ONLY used by the Process module which handles the commit itself.
    /// <returns>The Id for the created OutgoingMessage</returns>
    /// </summary>
    Task<Guid> EnqueueAsync(AcceptedWholesaleServicesMessageDto acceptedWholesaleServicesMessage, CancellationToken cancellationToken);
}
