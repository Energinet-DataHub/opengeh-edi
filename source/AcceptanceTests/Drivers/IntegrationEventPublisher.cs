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

namespace Energinet.DataHub.EDI.AcceptanceTests.Drivers;

internal sealed class IntegrationEventPublisher : IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;

    internal IntegrationEventPublisher(string connectionString, string topicName)
    {
        _client = new ServiceBusClient(
            connectionString,
            new ServiceBusClientOptions()
            {
                TransportType = ServiceBusTransportType.AmqpWebSockets, // Firewall is not open for AMQP and Therefore, needs to go over WebSockets.
            });
        _sender = _client.CreateSender(topicName);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeCoreAsync().ConfigureAwait(false);

        GC.SuppressFinalize(this);
    }

    internal Task PublishAsync(string eventName, byte[] eventPayload)
    {
        return _sender.SendMessageAsync(CreateIntegrationEventMessage(eventName, eventPayload));
    }

    private static ServiceBusMessage CreateIntegrationEventMessage(string eventName, byte[] eventPayload)
    {
        var messageId = Guid.NewGuid().ToString();

        var message = new ServiceBusMessage()
        {
            Body = new BinaryData(eventPayload),
            ContentType = "application/octet-stream",
            MessageId = messageId,
            Subject = eventName,
        };
        message.ApplicationProperties.Add("EventMinorVersion", 0);
        return message;
    }

    private async ValueTask DisposeCoreAsync()
    {
        await _client.DisposeAsync().ConfigureAwait(false);
        await _sender.DisposeAsync().ConfigureAwait(false);
    }
}
