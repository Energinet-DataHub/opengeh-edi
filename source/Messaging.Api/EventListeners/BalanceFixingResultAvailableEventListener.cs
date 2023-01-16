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

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Messaging.Api.EventListeners;

public class BalanceFixingResultAvailableEventListener
{
    private readonly ILogger<BalanceFixingResultAvailableEventListener> _logger;

    public BalanceFixingResultAvailableEventListener(ILogger<BalanceFixingResultAvailableEventListener> logger)
    {
        _logger = logger;
    }

    [Function(nameof(BalanceFixingResultAvailableEventListener))]
    public void Run(
        [ServiceBusTrigger(
            "%INTEGRATION_EVENTS_TOPIC_NAME%",
            "%BALANCE_FIXING_RESULT_AVAILABLE_EVENT_SUBSCRIPTION_NAME%",
            Connection = "SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_LISTENER")]
        byte[] eventData,
        FunctionContext context)
    {
        var processCompletedEvent =
            Energinet.DataHub.Wholesale.Contracts.Events.ProcessCompleted.Parser.ParseFrom(eventData);
        _logger.LogInformation($"Received ProcessCompleted event: {processCompletedEvent}");
    }
}
