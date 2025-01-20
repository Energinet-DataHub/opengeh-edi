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
                message.ApplicationProperties,
            },
        });

        _logger.LogInformation("Handling received enqueue actor messages service bus message.");

        var majorVersion = message.GetMajorVersion();
        if (majorVersion == EnqueueActorMessagesV1.MajorVersion)
        {
            await HandleV1Async(message).ConfigureAwait(false);
        }
        else
        {
            throw new ArgumentOutOfRangeException(
                nameof(majorVersion),
                majorVersion,
                $"Unsupported major version from service bus message (MessageId={message.MessageId}, Subject={message.Subject})");
        }
    }

    protected abstract Task EnqueueActorMessagesV1Async(EnqueueActorMessagesV1 enqueueActorMessages);

    private async Task HandleV1Async(ServiceBusReceivedMessage serviceBusMessage)
    {
        var enqueueActorMessages = serviceBusMessage.ParseBody<EnqueueActorMessagesV1>();

        using var enqueueActorMessagesLoggerScope = _logger.BeginScope(new
        {
            EnqueueMessages = new
            {
                enqueueActorMessages.OrchestrationName,
                enqueueActorMessages.OrchestrationVersion,
                OperatingIdentity = new
                {
                    ActorId = enqueueActorMessages.OrchestrationStartedByActorId,
                    UserId = enqueueActorMessages.HasOrchestrationStartedByUserId
                        ? enqueueActorMessages.OrchestrationStartedByUserId
                        : null,
                },
                enqueueActorMessages.DataType,
                enqueueActorMessages.DataFormat,
                enqueueActorMessages.OrchestrationInstanceId,
            },
        });

        await EnqueueActorMessagesV1Async(enqueueActorMessages)
            .ConfigureAwait(false);
    }
}
