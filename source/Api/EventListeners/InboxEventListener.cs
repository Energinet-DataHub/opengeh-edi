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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.Api.Configuration;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.Process.Infrastructure.InboxEvents;
using Microsoft.Azure.Functions.Worker;

namespace Energinet.DataHub.EDI.Api.EventListeners;

public class InboxEventListener
{
    private readonly ISerializer _jsonSerializer;
    private readonly InboxEventReceiver _inboxEventReceiver;

    public InboxEventListener(
        ISerializer jsonSerializer,
        InboxEventReceiver inboxEventReceiver)
    {
        _jsonSerializer = jsonSerializer;
        _inboxEventReceiver = inboxEventReceiver;
    }

    [Function(nameof(InboxEventListener))]
    public async Task RunAsync(
        [ServiceBusTrigger(
            "%EDI_INBOX_MESSAGE_QUEUE_NAME%",
            Connection = "SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_LISTENER")] byte[] message,
        FunctionContext context,
        CancellationToken hostCancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(context);

        context.BindingContext.BindingData.TryGetValue("MessageId", out var eventId);
        ArgumentNullException.ThrowIfNull(eventId);

        //Matching ADR-008 name convention
        context.BindingContext.BindingData.TryGetValue("Subject", out var eventName);
        ArgumentNullException.ThrowIfNull(eventName);

        var referenceId = GetReferenceId(context);
        await _inboxEventReceiver.ReceiveAsync(
            (string)eventId,
            (string)eventName,
            referenceId,
            message).ConfigureAwait(false);
    }

    private Guid GetReferenceId(FunctionContext context)
    {
        context.BindingContext.BindingData.TryGetValue("UserProperties", out var serviceBusMessageMetadata);

        if (serviceBusMessageMetadata is null)
        {
            throw new InvalidOperationException($"Service bus metadata must be specified as User Properties attributes");
        }

        var metadata = _jsonSerializer.Deserialize<InboxMessageMetadata>(serviceBusMessageMetadata.ToString() ?? throw new InvalidOperationException());
        return metadata.ReferenceId;
    }
}
