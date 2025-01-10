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

using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages;

/// <summary>
/// Base handler for enqueuing messages. Handles converting the <see cref="ServiceBusReceivedMessage"/> into
/// the expected <see cref="EnqueueMessagesDto"/> type.
/// </summary>
/// <param name="logger"></param>
public abstract class EnqueueMessagesHandlerBase(
    ILogger logger)
{
    private readonly ILogger _logger = logger;

    /// <summary>
    /// Enqueue the received service bus message sent from the Process Manager subsystem.
    /// The service bus message body must be of the type <see cref="EnqueueMessagesDto"/>.
    /// </summary>
    /// <param name="message"></param>
    public async Task EnqueueAsync(ServiceBusReceivedMessage message)
    {
        using var serviceBusMessageLoggerScope = _logger.BeginScope(new
        {
            ServiceBusMessage = new
            {
                message.MessageId,
                message.CorrelationId,
                message.Subject,
            },
        });

        var jsonMessage = message.Body.ToString();

        var enqueueActorMessages = EnqueueActorMessages.Parser.ParseJson(jsonMessage);

        if (enqueueActorMessages is null)
        {
            _logger.LogError(
                "Failed to parse service bus message body as JSON to type \"{EnqueueMessagesDto}\". Actual body value as string:\n{Body}",
                nameof(EnqueueActorMessages),
                jsonMessage);
            throw new ArgumentException($"Enqueue handler cannot parse received service bus message body to type \"{nameof(EnqueueActorMessages)}\"", nameof(message.Body));
        }

        using var enqueueActorMessagesLoggerScope = _logger.BeginScope(new
        {
            EnqueueMessages = new
            {
                enqueueActorMessages.OrchestrationName,
                enqueueActorMessages.OrchestrationVersion,
                OperatingIdentity = new
                {
                    ActorId = enqueueActorMessages.OrchestrationStartedByActorId,
                    UserId = enqueueActorMessages.OrchestrationStartedByUserId,
                },
                enqueueActorMessages.DataType,
            },
        });

        await EnqueueMessagesAsync(enqueueActorMessages)
            .ConfigureAwait(false);
    }

    protected abstract Task EnqueueMessagesAsync(EnqueueActorMessages enqueueActorMessages);

    protected TData DeserializeJsonInput<TData>(EnqueueActorMessages enqueueActorMessages)
    {
        var deserializeResult = JsonSerializer.Deserialize<TData>(enqueueActorMessages.JsonData);

        if (deserializeResult == null)
        {
            _logger.LogError(
                "Failed to deserialize EnqueueMessagesDto.JsonInput to type \"{Type}\". Actual JSON value:\n{JsonInput}",
                typeof(TData).Name,
                enqueueActorMessages.JsonData);
            throw new ArgumentException($"Cannot deserialize {nameof(EnqueueActorMessages)}.{nameof(enqueueActorMessages.JsonData)} to type {typeof(TData).Name}", nameof(enqueueActorMessages.JsonData));
        }

        return deserializeResult;
    }
}
