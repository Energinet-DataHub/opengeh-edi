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
using Microsoft.Azure.Functions.Worker;

namespace Energinet.DataHub.EDI.B2BApi.EventListeners;

public class IntegrationEventDeadLetterListener
{
    // TODO: Move to Messaging;
    // TODO: Decide if it should contain '/' or not?
    private const string DeadLetterQueueSuffix = "/$DeadLetterQueue";

    // TODO: Move to Messaging;
    private const string DeadLetterIsLoggedProperty = "DeadLetterIsLogged";

    public IntegrationEventDeadLetterListener()
    {
    }

    [Function(nameof(IntegrationEventDeadLetterListener))]
    public async Task RunAsync(
        [ServiceBusTrigger(
            $"%{IntegrationEventsOptions.SectionName}:{nameof(IntegrationEventsOptions.TopicName)}%",
            $"%{IntegrationEventsOptions.SectionName}:{nameof(IntegrationEventsOptions.SubscriptionName)}%{DeadLetterQueueSuffix}",
            Connection = ServiceBusNamespaceOptions.SectionName,
            AutoCompleteMessages = false)]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions)
    {
        message.ApplicationProperties.TryGetValue(DeadLetterIsLoggedProperty, out var isLogged);
        if (isLogged is null or (object)false)
        {
            // TODO: Log message HERE
            var propertiesToModify = new Dictionary<string, object>
            {
                [DeadLetterIsLoggedProperty] = true,
            };
            await messageActions.DeferMessageAsync(message, propertiesToModify).ConfigureAwait(false);
        }
    }
}
