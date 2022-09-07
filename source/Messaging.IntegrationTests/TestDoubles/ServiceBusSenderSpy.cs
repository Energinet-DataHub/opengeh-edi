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

namespace Messaging.IntegrationTests.TestDoubles
{
    public sealed class ServiceBusSenderSpy : IServiceBusSenderAdapter
    {
        public ServiceBusSenderSpy(string topicName)
        {
            TopicName = topicName;
        }

        public string TopicName { get; }

        public ServiceBusMessage? Message { get; private set; }

        public Task SendAsync(ServiceBusMessage message)
        {
            Message = message;
            return Task.CompletedTask;
        }

        #pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
