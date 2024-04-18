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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application.UseCases;

/// <summary>
/// Responsible for delegating messages to the correct receiver if a delegation relationship exists.
/// </summary>
public class DelegateMessage
{
    private readonly IMasterDataClient _masterDataClient;

    public DelegateMessage(IMasterDataClient masterDataClient)
    {
        _masterDataClient = masterDataClient;
    }

    /// <summary>
    /// If a delegation relationship exists, the message is delegated to the correct receiver.
    /// </summary>
    public async Task<OutgoingMessage> DelegateAsync(
        OutgoingMessage messageToEnqueue,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(messageToEnqueue);

        // TODO: Delegate rejected messages based on who made the request
        if (messageToEnqueue.DocumentType == DocumentType.RejectRequestAggregatedMeasureData || messageToEnqueue.DocumentType == DocumentType.RejectRequestWholesaleSettlement)
            return messageToEnqueue;

        if (string.IsNullOrEmpty(messageToEnqueue.GridAreaCode))
            throw new ArgumentException($"Grid area code is required to delegate outgoing message with id {messageToEnqueue.Id.Value}");

        var delegatedTo = await GetDelegatedReceiverAsync(
            messageToEnqueue.DocumentReceiver.Number,
            messageToEnqueue.DocumentReceiver.ActorRole,
            messageToEnqueue.GridAreaCode,
            messageToEnqueue.MessageCreatedFromProcess,
            cancellationToken).ConfigureAwait(false);

        if (delegatedTo is not null)
        {
            messageToEnqueue.DelegateTo(delegatedTo);
        }

        return messageToEnqueue;
    }

    private async Task<Receiver?> GetDelegatedReceiverAsync(
        ActorNumber delegatedByActorNumber,
        ActorRole delegatedByActorRole,
        string gridAreaCode,
        ProcessType messageCreatedFromProcess,
        CancellationToken cancellationToken)
    {
        var messageDelegation = await _masterDataClient.GetProcessDelegatedByAsync(
            delegatedByActorNumber,
            delegatedByActorRole,
            gridAreaCode,
            messageCreatedFromProcess,
            cancellationToken)
            .ConfigureAwait(false);

        return messageDelegation is not null
            ? Receiver.Create(messageDelegation.DelegatedTo.ActorNumber, messageDelegation.DelegatedTo.ActorRole)
            : null;
    }
}
