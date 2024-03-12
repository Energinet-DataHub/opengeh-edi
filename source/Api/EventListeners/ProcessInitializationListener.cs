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

using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.EDI.Process.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.Api.EventListeners;

public class ProcessInitializationListener
{
    private readonly ILogger<ProcessInitializationListener> _logger;
    private readonly IProcessClient _processClient;

    public ProcessInitializationListener(
        ILogger<ProcessInitializationListener> logger,
        IProcessClient processClient)
    {
        _processClient = processClient;
    }

    [Function(nameof(ProcessInitializationListener))]
    public async Task RunAsync(
        [ServiceBusTrigger(
            "%INCOMING_MESSAGES_QUEUE_NAME%",
            Connection = "SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_LISTENER")]
        ServiceBusReceivedMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        _logger.LogInformation("Initialization Listener details: {Message}", message);

        await _processClient.InitializeAsync(message.Subject, message.Body.ToArray()).ConfigureAwait(false);
    }
}
