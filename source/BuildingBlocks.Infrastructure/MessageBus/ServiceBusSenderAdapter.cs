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
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.MessageBus;

public sealed class ServiceBusSenderAdapter : IServiceBusSenderAdapter
{
    private readonly ServiceBusSender _serviceBusSender;

    public ServiceBusSenderAdapter(ServiceBusClient serviceBusClient, string topicName)
    {
        ArgumentNullException.ThrowIfNull(serviceBusClient);
        TopicName = topicName;
        _serviceBusSender = serviceBusClient.CreateSender(topicName);
    }

    public string TopicName { get; }

    public Task SendAsync(ServiceBusMessage message, CancellationToken cancellationToken)
    {
        return _serviceBusSender.SendMessageAsync(message, cancellationToken);
    }

    public Task SendAsync(ReadOnlyCollection<ServiceBusMessage> messages, CancellationToken cancellationToken)
    {
        return _serviceBusSender.SendMessagesAsync(messages, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _serviceBusSender.DisposeAsync().ConfigureAwait(false);
        Dispose();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
