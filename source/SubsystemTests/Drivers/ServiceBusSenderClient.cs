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

using Azure.Messaging.ServiceBus;

namespace Energinet.DataHub.EDI.SubsystemTests.Drivers;

internal sealed class ServiceBusSenderClient : IAsyncDisposable
{
    private readonly ServiceBusSender _sender;

    public ServiceBusSenderClient(
        ServiceBusClient client,
        string queueOrTopicName)
    {
        _sender = client.CreateSender(queueOrTopicName);
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    internal async Task SendAsync(ServiceBusMessage message, CancellationToken cancellationToken)
    {
        await _sender.SendMessageAsync(message, cancellationToken).ConfigureAwait(false);
    }
}
