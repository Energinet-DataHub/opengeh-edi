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
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Queueing;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Queueing.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application;

public class OutgoingMessagesClient : IOutgoingMessagesClient
{
    private readonly MessagePeeker _messagePeeker;
    private readonly MessageDequeuer _messageDequeuer;
    private readonly MessageEnqueuer _messageEnqueuer;
    private readonly ActorMessageQueueContext _actorMessageQueueContext;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;
    private readonly ISerializer _serializer;

    public OutgoingMessagesClient(
        MessagePeeker messagePeeker,
        MessageDequeuer messageDequeuer,
        MessageEnqueuer messageEnqueuer,
        ActorMessageQueueContext actorMessageQueueContext,
        ISystemDateTimeProvider systemDateTimeProvider,
        ISerializer serializer)
    {
        _messagePeeker = messagePeeker;
        _messageDequeuer = messageDequeuer;
        _messageEnqueuer = messageEnqueuer;
        _actorMessageQueueContext = actorMessageQueueContext;
        _systemDateTimeProvider = systemDateTimeProvider;
        _serializer = serializer;
    }

    public async Task<DequeueRequestResultDto> DequeueAndCommitAsync(DequeueRequestDto request, CancellationToken cancellationToken)
    {
        var dequeueRequestResult = await _messageDequeuer.DequeueAsync(request, cancellationToken).ConfigureAwait(false);
        await _actorMessageQueueContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return dequeueRequestResult;
    }

    public async Task<PeekResultDto> PeekAndCommitAsync(PeekRequestDto request, CancellationToken cancellationToken)
    {
        var peekResult = await _messagePeeker.PeekAsync(request, cancellationToken).ConfigureAwait(false);
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
        var messageId = await _messageEnqueuer.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
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
        var messageId = await _messageEnqueuer.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
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

        var messageId = await _messageEnqueuer.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
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
        var messageId = await _messageEnqueuer.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
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
            await _messageEnqueuer.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
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
        var messageId = await _messageEnqueuer.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
        return messageId;
    }
}
