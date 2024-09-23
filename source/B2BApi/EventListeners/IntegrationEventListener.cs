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
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.Core.Messaging.Communication.Extensions.Options;
using Energinet.DataHub.Core.Messaging.Communication.Subscriber;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.B2BApi.EventListeners;

public class IntegrationEventListener(
    ILogger<IntegrationEventListener> logger,
    ISubscriber subscriber)
{
    private readonly ILogger<IntegrationEventListener> _logger = logger;
    private readonly ISubscriber _subscriber = subscriber;

    /// <summary>
    /// Receives messages from the integration event topic and processes them.
    /// </summary>
    /// <remarks>
    /// Retries are currently handled by the Service Bus to avoid blocking a Azure Function Worker.
    /// If the method fails to process a message, the Service Bus will automatically retry delivery of the message
    /// based on the retry policy configured for the Service Bus.
    /// </remarks>
    [Function(nameof(IntegrationEventListener))]
    public async Task RunAsync(
        [ServiceBusTrigger(
            $"%{IntegrationEventsOptions.SectionName}:{nameof(IntegrationEventsOptions.TopicName)}%",
            $"%{IntegrationEventsOptions.SectionName}:{nameof(IntegrationEventsOptions.SubscriptionName)}%",
            Connection = ServiceBusNamespaceOptions.SectionName)]
        ServiceBusReceivedMessage message)
    {
        var eventDetails = new EventDetails(message.MessageId, message.Subject);
        _logger.LogInformation("Integration event details: {EventDetails}", eventDetails);

        await _subscriber
            .HandleAsync(IntegrationEventServiceBusMessage.Create(message))
            .ConfigureAwait(false);
    }
}
