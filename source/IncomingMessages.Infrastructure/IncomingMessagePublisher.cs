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

using Azure.Messaging.ServiceBus;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Domain;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Abstractions;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Factories;
using Energinet.DataHub.EDI.Process.Interfaces;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure;

public class IncomingMessagePublisher
{
    private readonly AuthenticatedActor _authenticatedActor;
    private readonly ISerializer _serializer;
    private readonly ServiceBusSender _sender;

    public IncomingMessagePublisher(
        AuthenticatedActor authenticatedActor,
        IOptions<IncomingMessagesQueueOptions> options,
        IAzureClientFactory<ServiceBusSender> senderFactory,
        ISerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(senderFactory);
        _authenticatedActor = authenticatedActor;
        _serializer = serializer;

        _sender = senderFactory.CreateClient(options.Value.QueueName);
    }

    public async Task PublishAsync(
        IIncomingMessage incomingMessage,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(incomingMessage, nameof(incomingMessage));
        switch (incomingMessage)
        {
            case RequestAggregatedMeasureDataMessage requestAggregatedMeasureDataMessage:
                await SendInitializeAggregatedMeasureDataProcessAsync(InitializeAggregatedMeasureDataProcessDtoFactory.Create(requestAggregatedMeasureDataMessage), cancellationToken).ConfigureAwait(false);
                break;
            case RequestWholesaleServicesMessage wholesaleSettlementMessage:
                await SendInitializeWholesaleServicesProcessAsync(InitializeWholesaleServicesProcessDtoFactory.Create(wholesaleSettlementMessage), cancellationToken).ConfigureAwait(false);
                break;
            case MeteredDataForMeasurementPointMessage meteredDataForMeasurementPointMessage:
                await SendInitializeMeteredDataForMeasurementPointMessageProcessAsync(InitializeMeteredDataForMeasurementPointProcessDtoFactory.Create(meteredDataForMeasurementPointMessage, _authenticatedActor), cancellationToken).ConfigureAwait(false);
                break;
            default:
                throw new InvalidOperationException($"Unknown message type {incomingMessage.GetType().Name}");
        }
    }

    private async Task SendInitializeAggregatedMeasureDataProcessAsync(InitializeAggregatedMeasureDataProcessDto initializeAggregatedMeasureDataProcessDto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(initializeAggregatedMeasureDataProcessDto);

        var serviceBusMessage =
            new ServiceBusMessage(
                new BinaryData(_serializer.Serialize(initializeAggregatedMeasureDataProcessDto)))
            {
                Subject = nameof(InitializeAggregatedMeasureDataProcessDto),
            };

        await _sender.SendMessageAsync(serviceBusMessage, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendInitializeWholesaleServicesProcessAsync(InitializeWholesaleServicesProcessDto initializeWholesaleServicesProcessDto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(initializeWholesaleServicesProcessDto);

        var serviceBusMessage =
            new ServiceBusMessage(
                _serializer.Serialize(initializeWholesaleServicesProcessDto))
            {
                Subject = nameof(InitializeWholesaleServicesProcessDto),
            };

        await _sender.SendMessageAsync(serviceBusMessage, cancellationToken).ConfigureAwait(false);
    }

    private async Task SendInitializeMeteredDataForMeasurementPointMessageProcessAsync(InitializeMeteredDataForMeasurementPointMessageProcessDto initializeMeteredDataForMeasurementPointMessageProcessDto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(initializeMeteredDataForMeasurementPointMessageProcessDto);

        var serviceBusMessage =
            new ServiceBusMessage(
                _serializer.Serialize(initializeMeteredDataForMeasurementPointMessageProcessDto))
            {
                Subject = nameof(InitializeMeteredDataForMeasurementPointMessageProcessDto),
            };

        await _sender.SendMessageAsync(serviceBusMessage, cancellationToken).ConfigureAwait(false);
    }
}
