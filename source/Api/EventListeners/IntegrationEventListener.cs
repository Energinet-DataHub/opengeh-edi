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

using System;
using System.Threading.Tasks;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.Core.Messaging.Communication.Subscriber;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.Api.EventListeners;

public class IntegrationEventListener
{
    private readonly ILogger<IntegrationEventListener> _logger;
    private readonly ISubscriber _subscriber;

    public IntegrationEventListener(ILogger<IntegrationEventListener> logger, ISubscriber subscriber)
    {
        _logger = logger;
        _subscriber = subscriber;
    }

    [Function(nameof(IntegrationEventListener))]
    public async Task RunAsync(
        [ServiceBusTrigger(
            "IntegrationEvents__TopicName",
            "IntegrationEvents__SubscriptionName",
            Connection = "ServiceBus__ListenConnectionString")]
        byte[] eventData,
        FunctionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var eventDetails = context.ExtractEventDetails();
        _logger.LogInformation("Integration event details: {EventDetails}", eventDetails);

        await _subscriber
            .HandleAsync(IntegrationEventServiceBusMessage.Create(eventData, context.BindingContext.BindingData!))
            .ConfigureAwait(false);
    }
}
