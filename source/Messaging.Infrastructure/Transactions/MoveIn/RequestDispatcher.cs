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
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Messaging.Infrastructure.Configuration.MessageBus;
using Microsoft.Extensions.Azure;

namespace Messaging.Infrastructure.Transactions.MoveIn;

public class RequestDispatcher<TConfiguration> : IRequestDispatcher
    where TConfiguration
    : IServiceBusClientConfiguration
{
    private readonly Lazy<ServiceBusSender> _senderCreator;

    public RequestDispatcher(IAzureClientFactory<ServiceBusClient> serviceBusClientFactory, TConfiguration configuration)
    {
        if (serviceBusClientFactory == null) throw new ArgumentNullException(nameof(serviceBusClientFactory));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        var serviceBusClient = serviceBusClientFactory.CreateClient(configuration.ClientRegistrationName);
        _senderCreator = new Lazy<ServiceBusSender>(() => serviceBusClient.CreateSender(configuration.QueueName));
    }

    public async Task SendAsync(ServiceBusMessage message)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));
        await _senderCreator.Value.SendMessageAsync(message).ConfigureAwait(false);
    }
}
