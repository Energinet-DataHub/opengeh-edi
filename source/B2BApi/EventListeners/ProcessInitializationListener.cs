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
using BuildingBlocks.Application.Extensions.Options;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.Process.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.B2BApi.EventListeners;

public class ProcessInitializationListener
{
    private readonly ILogger<ProcessInitializationListener> _logger;
    private readonly IProcessClient _processClient;

    public ProcessInitializationListener(
        ILogger<ProcessInitializationListener> logger,
        IProcessClient processClient)
    {
        _logger = logger;
        _processClient = processClient;
    }

    /// <summary>
    /// Receives messages from the inbox queue and forwards them to the inbox event receiver.
    /// </summary>
    /// <remarks>
    /// Retries are currently handled by the Service Bus to avoid blocking a Azure Function Worker.
    /// If the method fails to process a message, the Service Bus will automatically retry delivery of the message
    /// based on the retry policy configured for the Service Bus.
    /// </remarks>
    [Function(nameof(ProcessInitializationListener))]
    public async Task RunAsync(
        [ServiceBusTrigger(
            $"%{IncomingMessagesQueueOptions.SectionName}:{nameof(IncomingMessagesQueueOptions.QueueName)}%",
            Connection = ServiceBusOptions.SectionName)]
        ServiceBusReceivedMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        _logger.LogInformation("Initialization Listener details: {Message}", message);

        await _processClient.InitializeAsync(message.Subject, message.Body.ToArray()).ConfigureAwait(false);
    }
}
