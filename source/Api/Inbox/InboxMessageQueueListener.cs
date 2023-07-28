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
using System.Threading;
using System.Threading.Tasks;
using Api.Configuration;
using Application.Configuration;
using Infrastructure.Configuration.InboxEvents;
using Infrastructure.Configuration.Serialization;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Api.Inbox;

public class InboxMessageQueueListener
{
    private readonly ILogger<InboxMessageQueueListener> _logger;
    private readonly ISerializer _jsonSerializer;
    private readonly ICorrelationContext _correlationContext;
    private readonly InboxEventReceiver _inboxEventReceiver;

    public InboxMessageQueueListener(
        ISerializer jsonSerializer,
        ICorrelationContext correlationContext,
        ILogger<InboxMessageQueueListener> logger,
        InboxEventReceiver inboxEventReceiver)
    {
        _logger = logger;
        _jsonSerializer = jsonSerializer;
        _correlationContext = correlationContext;
        _inboxEventReceiver = inboxEventReceiver;
    }

    [Function(nameof(InboxMessageQueueListener))]
    public async Task RunAsync(
        [ServiceBusTrigger("%EDI_INBOX_MESSAGE_QUEUE_NAME%", Connection = "SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_LISTENER")] byte[] message,
        FunctionContext context,
        CancellationToken hostCancellationToken)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        if (context == null) throw new ArgumentNullException(nameof(context));

        SetCorrelationIdFromServiceBusMessage(context);

        context.BindingContext.BindingData.TryGetValue("RequestId", out var eventId);
        ArgumentNullException.ThrowIfNull(eventId);

        //Matching ADR-008 name convention
        context.BindingContext.BindingData.TryGetValue("Subject", out var eventName);
        ArgumentNullException.ThrowIfNull(eventName);
        await _inboxEventReceiver.ReceiveAsync(
            (string)eventId,
            (string)eventName,
            message).ConfigureAwait(false);
    }

    private void SetCorrelationIdFromServiceBusMessage(FunctionContext context)
    {
        context.BindingContext.BindingData.TryGetValue("UserProperties", out var serviceBusMessageMetadata);

        if (serviceBusMessageMetadata is null)
        {
            throw new InvalidOperationException($"Service bus metadata must be specified as User Properties attributes");
        }

        var metadata = _jsonSerializer.Deserialize<ServiceBusMessageMetadata>(serviceBusMessageMetadata.ToString() ?? throw new InvalidOperationException());
        _correlationContext.SetId(metadata.CorrelationID ?? throw new InvalidOperationException("Service bus metadata property CorrelationID is missing"));

        _logger.LogInformation("Dequeued service bus message with correlation id: " + _correlationContext.Id ?? string.Empty);
    }
}
