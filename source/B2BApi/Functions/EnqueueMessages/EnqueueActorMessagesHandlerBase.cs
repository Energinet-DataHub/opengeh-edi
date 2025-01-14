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
using DurableTask.Core.Common;
using Energinet.DataHub.ProcessManager.Abstractions.Contracts;
using Energinet.DataHub.ProcessManager.Shared.Extensions;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.EDI.B2BApi.Functions.EnqueueMessages;

/// <summary>
/// Base handler for enqueuing messages. Handles converting the <see cref="ServiceBusReceivedMessage"/> into
/// the expected type.
/// </summary>
/// <param name="logger"></param>
public abstract class EnqueueActorMessagesHandlerBase(
    ILogger logger)
{
    private readonly ILogger _logger = logger;

    /// <summary>
    /// Enqueue the received service bus message sent from the Process Manager subsystem.
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

        var bodyFormat = message.GetBodyFormat();
        var majorVersion = message.GetMajorVersion();
        if (majorVersion == EnqueueActorMessagesV1.MajorVersion)
        {
            await HandleV1Async(message, bodyFormat).ConfigureAwait(false);
        }
        else
        {
            throw new ArgumentOutOfRangeException(
                nameof(majorVersion),
                majorVersion,
                $"Unsupported major version from service bus message (MessageId={message.MessageId}, Subject={message.Subject})");
        }
    }

    protected abstract Task EnqueueMessagesAsync(EnqueueActorMessagesV1 enqueueActorMessages);

    protected TData DeserializeMessageData<TData>(string dataFormat, string data)
    {
        var deserializeResult = dataFormat switch
        {
            "application/json" => JsonSerializer.Deserialize<TData>(data),
            _ => throw new ArgumentOutOfRangeException(
                nameof(dataFormat),
                dataFormat,
                "Unhandled data format from received enqueue actor messages"),
        };

        if (deserializeResult == null)
        {
            _logger.LogError(
                "Failed to deserialize EnqueueActorMessagesV1.Data to type \"{Type}\" (format: {DataFormat}). Actual value:\n{Data}",
                typeof(TData).Name,
                dataFormat,
                data.Truncate(maxLength: 1000));
            throw new ArgumentException($"Cannot deserialize {nameof(EnqueueActorMessagesV1)}.{nameof(data)} to type {typeof(TData).Name}", nameof(data));
        }

        return deserializeResult;
    }

    private async Task HandleV1Async(ServiceBusReceivedMessage serviceBusMessage, string messageBodyFormat)
    {
        var enqueueActorMessages = messageBodyFormat switch
        {
            "application/json" => EnqueueActorMessagesV1.Parser.ParseJson(serviceBusMessage.Body.ToString()),
            "application/octet-stream" => EnqueueActorMessagesV1.Parser.ParseFrom(serviceBusMessage.Body),
            _ => throw new ArgumentOutOfRangeException(
                nameof(messageBodyFormat),
                messageBodyFormat,
                $"Unhandled message body format (MessageId={serviceBusMessage.MessageId}, Subject={serviceBusMessage.Subject})"),
        };

        if (enqueueActorMessages is null)
        {
            _logger.LogError(
                "Failed to parse service bus message body as JSON to type \"{EnqueueMessagesDto}\". Actual body value as string:\n{Body}",
                nameof(EnqueueActorMessagesV1),
                serviceBusMessage.Body.ToString());
            throw new ArgumentException($"Enqueue handler cannot parse received service bus message body to type \"{nameof(EnqueueActorMessagesV1)}\"", nameof(serviceBusMessage.Body));
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
}
