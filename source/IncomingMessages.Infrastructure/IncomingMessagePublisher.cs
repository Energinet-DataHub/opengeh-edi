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
using Energinet.DataHub.EDI.IncomingMessages.Domain.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Factories;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.ProcessManager;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.EDI.IncomingMessages.Infrastructure;

public class IncomingMessagePublisher
{
    private readonly AuthenticatedActor _authenticatedActor;
    private readonly IRequestProcessOrchestrationStarter _requestProcessOrchestrationStarter;
    private readonly ForwardMeteredDataOrchestrationStarter _forwardMeteredDataOrchestrationStarter;

    public IncomingMessagePublisher(
        AuthenticatedActor authenticatedActor,
        IOptions<IncomingMessagesQueueOptions> options,
        IAzureClientFactory<ServiceBusSender> senderFactory,
        IRequestProcessOrchestrationStarter requestProcessOrchestrationStarter,
        ForwardMeteredDataOrchestrationStarter forwardMeteredDataOrchestrationStarter)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(senderFactory);
        _authenticatedActor = authenticatedActor;
        _requestProcessOrchestrationStarter = requestProcessOrchestrationStarter;
        _forwardMeteredDataOrchestrationStarter = forwardMeteredDataOrchestrationStarter;
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
            case MeteredDataForMeteringPointMessageBase meteredDataForMeteringPointMessage:
                await SendInitializeMeteredDataForMeteringPointMessageProcessAsync(InitializeMeteredDataForMeteringPointProcessDtoFactory.Create(meteredDataForMeteringPointMessage, _authenticatedActor), cancellationToken).ConfigureAwait(false);
                break;
            default:
                throw new InvalidOperationException($"Unknown message type {incomingMessage.GetType().Name}");
        }
    }

    private async Task SendInitializeAggregatedMeasureDataProcessAsync(InitializeAggregatedMeasureDataProcessDto initializeAggregatedMeasureDataProcessDto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(initializeAggregatedMeasureDataProcessDto);

        await _requestProcessOrchestrationStarter.StartRequestAggregatedMeasureDataOrchestrationAsync(
                    initializeAggregatedMeasureDataProcessDto,
                    cancellationToken)
                .ConfigureAwait(false);
    }

    private async Task SendInitializeWholesaleServicesProcessAsync(InitializeWholesaleServicesProcessDto initializeWholesaleServicesProcessDto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(initializeWholesaleServicesProcessDto);

        await _requestProcessOrchestrationStarter.StartRequestWholesaleServicesOrchestrationAsync(
                    initializeWholesaleServicesProcessDto,
                    cancellationToken)
                .ConfigureAwait(false);
    }

    private async Task SendInitializeMeteredDataForMeteringPointMessageProcessAsync(InitializeMeteredDataForMeteringPointMessageProcessDto initializeMeteredDataForMeteringPointMessageProcessDto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(initializeMeteredDataForMeteringPointMessageProcessDto);

        await _forwardMeteredDataOrchestrationStarter.StartForwardMeteredDataOrchestrationAsync(
                initializeMeteredDataForMeteringPointMessageProcessDto,
                cancellationToken)
            .ConfigureAwait(false);
    }
}
