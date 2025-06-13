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
using Energinet.DataHub.Core.Messaging.Communication.Extensions.Options;
using Energinet.DataHub.EDI.B2BApi.Configuration;
using Energinet.DataHub.EDI.B2BApi.MeasurementsSynchronization;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.FeatureManagement;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace Energinet.DataHub.EDI.B2BApi.Functions;

/// <summary>
/// Service Bus Trigger to process measurements synchronization from DataHub 2.
/// </summary>
public class MeasurementsSynchronizationTrigger(
    ILogger<MeasurementsSynchronizationTrigger> logger,
    IFeatureManager featureManager,
    IMeasurementsJsonToEbixStreamWriter measurementsJsonToEbixStreamWriter,
    IIncomingMessageClient incomingMessageClient,
    AuthenticatedActor authenticatedActor)
{
    private readonly ILogger<MeasurementsSynchronizationTrigger> _logger = logger;
    private readonly IFeatureManager _featureManager = featureManager;
    private readonly IMeasurementsJsonToEbixStreamWriter _measurementsJsonToEbixStreamWriter = measurementsJsonToEbixStreamWriter;
    private readonly IIncomingMessageClient _incomingMessageClient = incomingMessageClient;
    private readonly AuthenticatedActor _authenticatedActor = authenticatedActor;

    [Function(nameof(MeasurementsSynchronizationTrigger))]
    public async Task RunAsync(
        [ServiceBusTrigger(
            $"%{MeasurementsSynchronizationOptions.SectionName}:{nameof(MeasurementsSynchronizationOptions.TopicName)}%",
            $"%{MeasurementsSynchronizationOptions.SectionName}:{nameof(MeasurementsSynchronizationOptions.TimeSeriesSync_SubscriptionName)}%",
            Connection = ServiceBusNamespaceOptions.SectionName)]
        ServiceBusReceivedMessage message,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received message for measurements synchronization: {MessageId}", message.MessageId);

        if (await _featureManager.SyncMeasurementsAsync().ConfigureAwait(false))
        {
            var streamAndSender = await _measurementsJsonToEbixStreamWriter.WriteStreamAsync(message.Body).ConfigureAwait(false);

            _authenticatedActor.SetAuthenticatedActor(new ActorIdentity(ActorNumber.Create(streamAndSender.Sender), Restriction.Owned, ActorRole.GridAccessProvider, null,  null));

            var responseMessage = await _incomingMessageClient.ReceiveIncomingMarketMessageAsync(
                    new IncomingMarketMessageStream(streamAndSender.Document),
                    DocumentFormat.Ebix,
                    IncomingDocumentType.NotifyValidatedMeasureData,
                    DocumentFormat.Ebix,
                    cancellationToken)
                .ConfigureAwait(false);

            if (responseMessage.IsErrorResponse)
            {
                _logger.LogWarning("Received error response for message {MessageId}: {ErrorMessage}", message.MessageId, responseMessage.MessageBody);
            }
        }
    }
}
