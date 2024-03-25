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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application;

public class MessageDelegator
{
    private readonly IMasterDataClient _masterDataClient;

    public MessageDelegator(IMasterDataClient masterDataClient)
    {
        _masterDataClient = masterDataClient;
    }

    public async Task<OutgoingMessage> DelegateAsync(
        OutgoingMessage messageToEnqueue,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(messageToEnqueue);

        // Delegation is only relevant for messages with a grid area code. E.g. not reject messages.
        if (messageToEnqueue.GridAreaCode is null)
        {
            return messageToEnqueue;
        }

        var delegatedTo = await GetDelegationReceiverAsync(
            messageToEnqueue.ReceiverId,
            messageToEnqueue.ReceiverRole,
            messageToEnqueue.GridAreaCode,
            messageToEnqueue.DocumentType,
            cancellationToken).ConfigureAwait(false);

        if (delegatedTo is not null)
        {
            messageToEnqueue.DelegateTo(delegatedTo);
        }

        return messageToEnqueue;
    }

    private static DelegatedProcess MapToDelegated(DocumentType documentType)
    {
        return documentType.Name switch
        {
            nameof(DocumentType.NotifyAggregatedMeasureData) => DelegatedProcess.ProcessReceiveEnergyResults,
            nameof(DocumentType.NotifyWholesaleServices) => DelegatedProcess.ProcessReceiveEnergyResults,
            _ => throw new InvalidOperationException("Document type is not supported for delegation"),
        };
    }

    private async Task<Receiver?> GetDelegationReceiverAsync(
        ActorNumber delegatedByActorNumber,
        ActorRole delegatedByActorRole,
        string gridAreaCode,
        DocumentType documentType,
        CancellationToken cancellationToken)
    {
        var messageDelegation = await _masterDataClient.GetProcessDelegationAsync(
            delegatedByActorNumber,
            delegatedByActorRole,
            gridAreaCode,
            MapToDelegated(documentType),
            cancellationToken)
            .ConfigureAwait(false);

        return messageDelegation is not null
            ? Receiver.Create(messageDelegation.DelegatedTo.ActorNumber, messageDelegation.DelegatedTo.ActorRole)
            : null;
    }
}
