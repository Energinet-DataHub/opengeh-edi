﻿// Copyright 2020 Energinet DataHub A/S
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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application.UseCases;

/// <summary>
/// Responsible for delegating messages to the correct receiver if a delegation relationship exists.
/// </summary>
public class DelegateMessage
{
    private readonly IMasterDataClient _masterDataClient;
    private readonly ILogger<DelegateMessage> _logger;

    public DelegateMessage(IMasterDataClient masterDataClient, ILogger<DelegateMessage> logger)
    {
        _masterDataClient = masterDataClient;
        _logger = logger;
    }

    /// <summary>
    /// If a delegation relationship exists, the message is delegated to the correct receiver.
    /// </summary>
    public async Task<OutgoingMessage> DelegateAsync(
        OutgoingMessage messageToEnqueue,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(messageToEnqueue);

        // Do not delegate outgoing message if it is created from a request,
        // because the receiver must be the same as the one who made the request
        if (messageToEnqueue.MessageCreatedFromProcess == ProcessType.RequestWholesaleResults
            || messageToEnqueue.MessageCreatedFromProcess == ProcessType.RequestEnergyResults)
        {
            return messageToEnqueue;
        }

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
            new(delegatedByActorNumber, delegatedByActorRole),
            gridAreaCode,
            messageCreatedFromProcess,
            cancellationToken)
            .ConfigureAwait(false);

        var delegatedReceiver = messageDelegation is not null
            ? Receiver.Create(messageDelegation.DelegatedTo.ActorNumber, messageDelegation.DelegatedTo.ActorRole)
            : null;

        if (delegatedReceiver is not null && messageDelegation is not null)
        {
            _logger.LogInformation(
                "Message was delegated from {FromActorNumber} to {ToActorNumber}, with role {ActorRole} based on delegation with sequence number {SequenceNumber}",
                delegatedByActorNumber.Value,
                delegatedReceiver.Number.Value,
                delegatedByActorRole.Code,
                messageDelegation.SequenceNumber);
        }

        return delegatedReceiver;
    }
}
