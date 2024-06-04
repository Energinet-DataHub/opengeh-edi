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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Application.UseCases;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Factories;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application;

public class OutgoingMessagesClient : IOutgoingMessagesClient
{
    private readonly PeekMessage _peekMessage;
    private readonly DequeueMessage _dequeueMessage;
    private readonly EnqueueMessage _enqueueMessage;
    private readonly ActorMessageQueueContext _actorMessageQueueContext;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly ISerializer _serializer;
    private readonly IMasterDataClient _masterDataClient;
    private readonly EnergyResultEnumerator _energyResultEnumerator;

    public OutgoingMessagesClient(
        PeekMessage peekMessage,
        DequeueMessage dequeueMessage,
        EnqueueMessage enqueueMessage,
        ActorMessageQueueContext actorMessageQueueContext,
        ISystemDateTimeProvider systemDateTimeProvider,
        ISerializer serializer,
        IMasterDataClient masterDataClient,
        EnergyResultEnumerator energyResultEnumerator)
    {
        _peekMessage = peekMessage;
        _dequeueMessage = dequeueMessage;
        _enqueueMessage = enqueueMessage;
        _actorMessageQueueContext = actorMessageQueueContext;
        _systemDateTimeProvider = systemDateTimeProvider;
        _serializer = serializer;
        _masterDataClient = masterDataClient;
        _energyResultEnumerator = energyResultEnumerator;
    }

    public async Task<DequeueRequestResultDto> DequeueAndCommitAsync(DequeueRequestDto request, CancellationToken cancellationToken)
    {
        var dequeueRequestResult = await _dequeueMessage.DequeueAsync(request, cancellationToken).ConfigureAwait(false);
        await _actorMessageQueueContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return dequeueRequestResult;
    }

    public async Task<PeekResultDto> PeekAndCommitAsync(PeekRequestDto request, CancellationToken cancellationToken)
    {
        var peekResult = await _peekMessage.PeekAsync(request, cancellationToken).ConfigureAwait(false);
        await _actorMessageQueueContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return peekResult;
    }

    public async Task<OutgoingMessageId> EnqueueAsync(
        AcceptedEnergyResultMessageDto acceptedEnergyResultMessage,
        CancellationToken cancellationToken)
    {
        var message = OutgoingMessage.CreateMessage(
            acceptedEnergyResultMessage,
            _serializer,
            _systemDateTimeProvider.Now());
        var messageId = await _enqueueMessage.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
        return messageId;
    }

    public async Task<OutgoingMessageId> EnqueueAsync(
        RejectedEnergyResultMessageDto rejectedEnergyResultMessage,
        CancellationToken cancellationToken)
    {
        var message = OutgoingMessage.CreateMessage(
            rejectedEnergyResultMessage,
            _serializer,
            _systemDateTimeProvider.Now());
        var messageId = await _enqueueMessage.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
        return messageId;
    }

    public async Task<OutgoingMessageId> EnqueueAsync(
        RejectedWholesaleServicesMessageDto rejectedWholesaleServicesMessage,
        CancellationToken cancellationToken)
    {
        var message = OutgoingMessage.CreateMessage(
            rejectedWholesaleServicesMessage,
            _serializer,
            _systemDateTimeProvider.Now());

        var messageId = await _enqueueMessage.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
        return messageId;
    }

    public async Task<OutgoingMessageId> EnqueueAndCommitAsync(
        EnergyResultMessageDto energyResultMessage,
        CancellationToken cancellationToken)
    {
        var message = OutgoingMessage.CreateMessage(
            energyResultMessage,
            _serializer,
            _systemDateTimeProvider.Now());
        var messageId = await _enqueueMessage.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
        await _actorMessageQueueContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return messageId;
    }

    public virtual async Task EnqueueAndCommitAsync(
        WholesaleServicesMessageDto wholesaleServicesMessage,
        CancellationToken cancellationToken)
    {
        var messages = OutgoingMessage.CreateMessages(
            wholesaleServicesMessage,
            _serializer,
            _systemDateTimeProvider.Now());
        foreach (var message in messages)
        {
            await _enqueueMessage.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
        }

        await _actorMessageQueueContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<OutgoingMessageId> EnqueueAsync(
        AcceptedWholesaleServicesMessageDto acceptedWholesaleServicesMessage,
        CancellationToken cancellationToken)
    {
        var message = OutgoingMessage.CreateMessage(
            acceptedWholesaleServicesMessage,
            _serializer,
            _systemDateTimeProvider.Now());
        var messageId = await _enqueueMessage.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
        return messageId;
    }

    public async Task<OutgoingMessageId> EnqueueAndCommitAsync(
        WholesaleServicesTotalSumMessageDto wholesaleServicesTotalSumMessage,
        CancellationToken cancellationToken)
    {
        var message = OutgoingMessage.CreateMessage(
            wholesaleServicesTotalSumMessage,
            _serializer,
            _systemDateTimeProvider.Now());
        var messageId = await _enqueueMessage.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
        await _actorMessageQueueContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return messageId;
    }

    public async Task<int> EnqueueEnergyResultsForGridAreaOwnersAsync(EnqueueMessagesInputDto input)
    {
        var numberOfEnqueuedMessages = 0;

        // TODO: Decide "view" / "query" based on calculation type
        await foreach (var energyResult in _energyResultEnumerator.GetAsync(input.CalculationId))
        {
            var receiverNumber = await _masterDataClient.GetGridOwnerForGridAreaCodeAsync(energyResult.GridAreaCode, CancellationToken.None).ConfigureAwait(false);
            var energyResultMessage = EnergyResultMessageDtoFactory.CreateAsync(EventId.From(input.EventId), energyResult, receiverNumber);
            await EnqueueAndCommitAsync(energyResultMessage, CancellationToken.None);
            numberOfEnqueuedMessages++;
        }

        return numberOfEnqueuedMessages;
    }
}
