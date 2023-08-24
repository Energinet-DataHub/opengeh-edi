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
using Energinet.DataHub.Core.Messaging.Communication.Subscriber;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.Core.Messaging.Communication.Internal.Subscriber;

internal sealed class ServiceBusProcessorFactory : IServiceBusProcessorFactory, IAsyncDisposable
{
    private readonly IOptions<SubscriberWorkerOptions> _options;

    private ServiceBusClient? _serviceBusClient;

    public ServiceBusProcessorFactory(IOptions<SubscriberWorkerOptions> options)
    {
        _options = options;
    }

    public ServiceBusProcessor CreateProcessor(string topicName, string subscriptionName)
    {
        _serviceBusClient ??= new ServiceBusClient(_options.Value.ServiceBusConnectionString);
        return _serviceBusClient.CreateProcessor(topicName, subscriptionName, new ServiceBusProcessorOptions
        {
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            MaxConcurrentCalls = _options.Value.MaxConcurrentCalls,
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_serviceBusClient != null)
        {
            await _serviceBusClient.DisposeAsync();
            _serviceBusClient = null;
        }
    }
}
