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

namespace Energinet.DataHub.BuildingBlocks.Tests.TestDoubles;

public sealed class ServiceBusSenderSpy : ServiceBusSender
{
    public ServiceBusSenderSpy(string queueOrTopicName)
    {
        QueueOrTopicName = queueOrTopicName;
        MessagesSent = [];
    }

    public bool ShouldFail { get; set; }

    public string QueueOrTopicName { get; }

    public ServiceBusMessage? LatestMessage => MessagesSent.LastOrDefault();

    public ICollection<ServiceBusMessage> MessagesSent { get; private set; }

    public bool MessageSent { get; private set; }

    public override Task SendMessageAsync(ServiceBusMessage message, CancellationToken cancellationToken = default)
    {
        if (ShouldFail)
            throw new ServiceBusException();

        MessagesSent.Add(message);
        MessageSent = true;

        return Task.CompletedTask;
    }

    public override Task SendMessagesAsync(IEnumerable<ServiceBusMessage> messages, CancellationToken cancellationToken = default)
    {
        return ShouldFail
            ? throw new ServiceBusException()
            : Task.CompletedTask;
    }

    public override Task SendMessagesAsync(ServiceBusMessageBatch messageBatch, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Spy doesn't support this operation.");
    }

    public void Reset()
    {
        MessageSent = false;
        MessagesSent.Clear();
    }
}
