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
using Energinet.DataHub.EnergySupplying.IntegrationEvents;
using Messaging.Application.Configuration.Commands;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Infrastructure.Configuration.InternalCommands;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Messaging.Api.EventListeners;

public class ConsumerMovedInListener
{
    private readonly ILogger<ConsumerMovedInListener> _logger;
    private readonly CommandSchedulerFacade _commandScheduler;

    public ConsumerMovedInListener(ILogger<ConsumerMovedInListener> logger, CommandSchedulerFacade commandScheduler)
    {
        _logger = logger;
        _commandScheduler = commandScheduler;
    }

    [Function("ConsumerMovedInListener")]
    public async Task RunAsync(
        [ServiceBusTrigger("%INTEGRATION_EVENT_TOPIC_NAME%", "%CONSUMER_MOVED_IN_EVENT_SUBSCRIPTION_NAME%", Connection = "SERVICE_BUS_CONNECTION_STRING_FOR_INTEGRATION_EVENTS_LISTENER")] byte[] data,
        FunctionContext context)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (context == null) throw new ArgumentNullException(nameof(context));

        var consumerMovedIn = ConsumerMovedIn.Parser.ParseFrom(data);
        _logger.LogInformation($"Received consumer moved in event: {consumerMovedIn}");
        await _commandScheduler.EnqueueAsync(new SetConsumerHasMovedIn(consumerMovedIn.ProcessId))
            .ConfigureAwait(false);
    }
}
