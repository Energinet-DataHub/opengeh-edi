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

using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;

namespace Communication.Internal;

public class ServiceBusQueueSenderProviderAlternative : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, ServiceBusSender> _senders = new();
    private readonly object _sendersLock = new();
    private readonly ServiceBusClient _serviceBusClient;

    public ServiceBusQueueSenderProviderAlternative(ServiceBusClient serviceBusClient)
    {
        _serviceBusClient = serviceBusClient;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var sender in _senders)
        {
            await sender.Value.DisposeAsync().ConfigureAwait(false);
        }

        GC.SuppressFinalize(this);
    }

    public ServiceBusSender GetQueueSender(string queueName)
    {
        ArgumentNullException.ThrowIfNull(queueName);

        var key = queueName.ToUpperInvariant();
        lock (_sendersLock)
        {
            _senders.TryGetValue(key, out var sender);
            if (sender is null)
            {
                sender = _serviceBusClient.CreateSender(queueName);
                _senders.TryAdd(key, sender);
            }

            return sender;
        }
    }
}
