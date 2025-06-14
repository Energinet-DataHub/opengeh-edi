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

using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Application.UseCases;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Dequeue;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.EnergyResultMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.EnergyResultMessages.Request;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MeteredDataForMeteringPoint;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MeteredDataForMeteringPoint.Request;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.MissingMeasurementMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.Peek;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.WholesaleResultMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models.WholesaleResultMessages.Request;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application;

public class OutgoingMessagesClient : IOutgoingMessagesClient
{
    private readonly PeekMessage _peekMessage;
    private readonly DequeueMessage _dequeueMessage;
    private readonly EnqueueMessage _enqueueMessage;
    private readonly IActorMessageQueueContext _actorMessageQueueContext;
    private readonly IClock _clock;
    private readonly ISerializer _serializer;

    public OutgoingMessagesClient(
        PeekMessage peekMessage,
        DequeueMessage dequeueMessage,
        EnqueueMessage enqueueMessage,
        IActorMessageQueueContext actorMessageQueueContext,
        IClock clock,
        ISerializer serializer)
    {
        _peekMessage = peekMessage;
        _dequeueMessage = dequeueMessage;
        _enqueueMessage = enqueueMessage;
        _actorMessageQueueContext = actorMessageQueueContext;
        _clock = clock;
        _serializer = serializer;
    }

    public async Task<DequeueRequestResultDto> DequeueAndCommitAsync(DequeueRequestDto request, CancellationToken cancellationToken)
    {
        var dequeueRequestResult = await _dequeueMessage.DequeueAsync(request, cancellationToken).ConfigureAwait(false);
        await _actorMessageQueueContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return dequeueRequestResult;
    }

    public async Task<PeekResultDto?> PeekAndCommitAsync(PeekRequestDto request, CancellationToken cancellationToken)
    {
        var peekResult = await _peekMessage.PeekAsync(request, cancellationToken).ConfigureAwait(false);
        await _actorMessageQueueContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return peekResult;
    }

    public async Task<Guid> EnqueueAsync(
        AcceptedEnergyResultMessageDto acceptedEnergyResultMessage,
        CancellationToken cancellationToken)
    {
        var message = OutgoingMessageFactory.CreateMessage(
            acceptedEnergyResultMessage,
            _serializer,
            _clock.GetCurrentInstant());
        var messageId = await _enqueueMessage.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
        return messageId.Value;
    }

    public async Task<Guid> EnqueueAsync(
        RejectedEnergyResultMessageDto rejectedEnergyResultMessage,
        CancellationToken cancellationToken)
    {
        var message = OutgoingMessageFactory.CreateMessage(
            rejectedEnergyResultMessage,
            _serializer,
            _clock.GetCurrentInstant());
        var messageId = await _enqueueMessage.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
        return messageId.Value;
    }

    public async Task<Guid> EnqueueAsync(
        RejectedWholesaleServicesMessageDto rejectedWholesaleServicesMessage,
        CancellationToken cancellationToken)
    {
        var message = OutgoingMessageFactory.CreateMessage(
            rejectedWholesaleServicesMessage,
            _serializer,
            _clock.GetCurrentInstant());

        var messageId = await _enqueueMessage.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
        return messageId.Value;
    }

    public async Task<Guid> EnqueueAndCommitAsync(
        EnergyResultPerGridAreaMessageDto messageDto,
        CancellationToken cancellationToken)
    {
        var message = OutgoingMessageFactory.CreateMessage(
            messageDto,
            _serializer,
            _clock.GetCurrentInstant());

        var messageId = await _enqueueMessage.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
        await _actorMessageQueueContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return messageId.Value;
    }

    public async Task<Guid> EnqueueAndCommitAsync(
        EnergyResultPerBalanceResponsibleMessageDto messageDto,
        CancellationToken cancellationToken)
    {
        var message = OutgoingMessageFactory.CreateMessage(
            messageDto,
            _serializer,
            _clock.GetCurrentInstant());

        var messageId = await _enqueueMessage.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
        await _actorMessageQueueContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return messageId.Value;
    }

    public async Task<IReadOnlyCollection<Guid>> EnqueueAndCommitAsync(
        EnergyResultPerEnergySupplierPerBalanceResponsibleMessageDto messageDto,
        CancellationToken cancellationToken)
    {
        var messages = OutgoingMessageFactory.CreateMessages(
            messageDto,
            _serializer,
            _clock.GetCurrentInstant());

        List<Guid> messageIds = [];
        foreach (var message in messages)
        {
            var messageId = await _enqueueMessage.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
            messageIds.Add(messageId.Value);
        }

        await _actorMessageQueueContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return messageIds;
    }

    public async Task<Guid> EnqueueAndCommitAsync(
        WholesaleTotalAmountMessageDto wholesaleTotalAmountMessageDto,
        CancellationToken cancellationToken)
    {
        var message = OutgoingMessageFactory.CreateMessage(
            wholesaleTotalAmountMessageDto,
            _serializer,
            _clock.GetCurrentInstant());
        var outgoingMessageId = await _enqueueMessage.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
        await _actorMessageQueueContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return outgoingMessageId.Value;
    }

    public async Task EnqueueAndCommitAsync(
        WholesaleAmountPerChargeMessageDto wholesaleAmountPerChargeMessageDto,
        CancellationToken cancellationToken)
    {
        var messages = OutgoingMessageFactory.CreateMessages(
            wholesaleAmountPerChargeMessageDto,
            _serializer,
            _clock.GetCurrentInstant());
        foreach (var message in messages)
        {
            await _enqueueMessage.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
        }

        await _actorMessageQueueContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task EnqueueAndCommitAsync(
        WholesaleMonthlyAmountPerChargeMessageDto wholesaleMonthlyAmountPerChargeMessageDto,
        CancellationToken cancellationToken)
    {
        var messages = OutgoingMessageFactory.CreateMessages(
            wholesaleMonthlyAmountPerChargeMessageDto,
            _serializer,
            _clock.GetCurrentInstant());
        foreach (var message in messages)
        {
            await _enqueueMessage.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
        }

        await _actorMessageQueueContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Guid> EnqueueAsync(
        AcceptedWholesaleServicesMessageDto acceptedWholesaleServicesMessage,
        CancellationToken cancellationToken)
    {
        var message = OutgoingMessageFactory.CreateMessage(
            acceptedWholesaleServicesMessage,
            _serializer,
            _clock.GetCurrentInstant());
        var messageId = await _enqueueMessage.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
        return messageId.Value;
    }

    public async Task<Guid> EnqueueAsync(
        AcceptedSendMeasurementsMessageDto acceptedSendMeasurementsMessageDto,
        CancellationToken cancellationToken)
    {
        var message = OutgoingMessageFactory.CreateMessage(
            acceptedSendMeasurementsMessageDto,
            _serializer,
            _clock.GetCurrentInstant());

        var messageId = await _enqueueMessage.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);

        return messageId.Value;
    }

    public async Task<Guid> EnqueueAsync(
        CalculatedMeasurementsMessageDto calculatedMeasurementsMessageDto,
        CancellationToken cancellationToken)
    {
        var message = OutgoingMessageFactory.CreateMessage(
            calculatedMeasurementsMessageDto,
            _serializer,
            _clock.GetCurrentInstant());

        var messageId = await _enqueueMessage.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);

        return messageId.Value;
    }

    public async Task<Guid> EnqueueAsync(
        RejectedSendMeasurementsMessageDto rejectedSendMeasurementsMessageDto,
        CancellationToken cancellationToken)
    {
        var message = OutgoingMessageFactory.CreateMessage(
            rejectedSendMeasurementsMessageDto,
            _serializer,
            _clock.GetCurrentInstant());

        var messageId = await _enqueueMessage.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);

        return messageId.Value;
    }

    public async Task<Guid> EnqueueAsync(
        MissingMeasurementMessageDto missingMeasurementMessageDto,
        CancellationToken cancellationToken)
    {
        var message = OutgoingMessageFactory.CreateMessage(
            missingMeasurementMessageDto,
            _serializer,
            _clock.GetCurrentInstant());

        var messageId = await _enqueueMessage.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);

        return messageId.Value;
    }

    public async Task<Guid> EnqueueAsync(
        RejectRequestMeasurementsMessageDto rejectRequestMeasurementsMessageDto,
        CancellationToken cancellationToken)
    {
        var message = OutgoingMessageFactory.CreateMessage(
            rejectRequestMeasurementsMessageDto,
            _serializer,
            _clock.GetCurrentInstant());
        var messageId = await _enqueueMessage.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);
        return messageId.Value;
    }

    public async Task<Guid> EnqueueAsync(
        AcceptedRequestMeasurementsMessageDto acceptedSendMeasurementsMessageDto,
        CancellationToken cancellationToken)
    {
        var message = OutgoingMessageFactory.CreateMessage(
            acceptedSendMeasurementsMessageDto,
            _serializer,
            _clock.GetCurrentInstant());

        var messageId = await _enqueueMessage.EnqueueAsync(message, cancellationToken).ConfigureAwait(false);

        return messageId.Value;
    }
}
