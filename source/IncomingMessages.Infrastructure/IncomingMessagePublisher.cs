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

using Azure.Messaging.ServiceBus;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.MessageBus;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Factories;
using Energinet.DataHub.EDI.Process.Interfaces;
using Microsoft.Extensions.Options;
using ServiceBusClientOptions = Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.Options.ServiceBusClientOptions;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure;

public class IncomingMessagePublisher
{
    private readonly ISerializer _serializer;
    private readonly IServiceBusSenderAdapter _senderCreator;

    public IncomingMessagePublisher(
        IServiceBusSenderFactory serviceBusSenderFactory,
        IOptions<ServiceBusClientOptions> options,
        ISerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(serviceBusSenderFactory);
        ArgumentNullException.ThrowIfNull(options);
        _serializer = serializer;

        _senderCreator = serviceBusSenderFactory.GetSender(options.Value.INCOMING_MESSAGES_QUEUE_NAME);
    }

    public async Task PublishAsync(
        IIncomingMessage incomingMessage,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(incomingMessage, nameof(incomingMessage));
        switch (incomingMessage)
        {
            case RequestAggregatedMeasureDataMessage requestAggregatedMeasureDataMessage:
                await SendRequestAggregatedMeasureDateAsync(RequestAggregatedMeasureDataDtoFactory.Create(requestAggregatedMeasureDataMessage), cancellationToken).ConfigureAwait(false);
                break;
            case RequestWholesaleServicesMessage wholesaleSettlementMessage:
                await SendRequestAggregatedMeasureDateAsync(RequestWholesaleServicesDtoFactory.Create(wholesaleSettlementMessage), cancellationToken).ConfigureAwait(false);
                break;
            default:
                throw new InvalidOperationException($"Unknown message type {incomingMessage.GetType().Name}");
        }
    }

    private async Task SendRequestAggregatedMeasureDateAsync(RequestAggregatedMeasureDataDto requestAggregatedMeasureDataDto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestAggregatedMeasureDataDto);

        var serviceBusMessage =
            new ServiceBusMessage(
                _serializer.Serialize(requestAggregatedMeasureDataDto))
            {
                Subject = nameof(RequestAggregatedMeasureDataDto),
            };

        await _senderCreator.SendAsync(serviceBusMessage, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendRequestAggregatedMeasureDateAsync(InitializeWholesaleServicesProcessDto initializeWholesaleServicesProcessDto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(initializeWholesaleServicesProcessDto);

        var serviceBusMessage =
            new ServiceBusMessage(
                _serializer.Serialize(initializeWholesaleServicesProcessDto))
            {
                Subject = nameof(InitializeWholesaleServicesProcessDto),
            };

        await _senderCreator.SendAsync(serviceBusMessage, cancellationToken).ConfigureAwait(false);
    }
}
