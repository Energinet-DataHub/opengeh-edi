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

namespace AcceptanceTest.Drivers;

internal sealed class InboxPublisher : IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;

    internal InboxPublisher(string connectionString, string topicName)
    {
        _client = new ServiceBusClient(connectionString);
        _sender = _client.CreateSender(topicName);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeCoreAsync().ConfigureAwait(false);

        GC.SuppressFinalize(this);
    }

    internal Task SendToInboxAsync(string eventName, byte[] eventPayload)
    {
        return _sender.SendMessageAsync(CreateInboxEventMessage(eventName, eventPayload));
    }

    private static ServiceBusMessage CreateInboxEventMessage(string eventName, byte[] eventPayload)
    {
        var messageId = Guid.NewGuid().ToString();

        var message = new ServiceBusMessage()
        {
            Body = new BinaryData(eventPayload),
            ContentType = "application/xml",
            MessageId = messageId,
            Subject = eventName,
        };
        message.ApplicationProperties.Add("ReferenceId", 0);
        return message;
    }

    private async ValueTask DisposeCoreAsync()
    {
        await _client.DisposeAsync().ConfigureAwait(false);
        await _sender.DisposeAsync().ConfigureAwait(false);
    }
}
