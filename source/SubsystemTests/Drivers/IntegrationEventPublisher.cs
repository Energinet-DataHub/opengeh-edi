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
using Dapper;
using Microsoft.Data.SqlClient;

namespace Energinet.DataHub.EDI.SubsystemTests.Drivers;

internal sealed class IntegrationEventPublisher : IAsyncDisposable
{
    private readonly string _dbConnectionString;
    private readonly ServiceBusSender _sender;

    internal IntegrationEventPublisher(ServiceBusClient client, string topicName, string dbConnectionString)
    {
        _dbConnectionString = dbConnectionString;
        _sender = client.CreateSender(topicName);
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    internal async Task PublishAsync(string eventName, byte[] eventPayload, bool waitForHandled)
    {
        var integrationEvent = CreateIntegrationEventMessage(eventName, eventPayload);
        await _sender
            .SendMessageAsync(integrationEvent)
            .ConfigureAwait(false);

        if (waitForHandled)
        {
            var timeout = TimeSpan.FromSeconds(30);
            var timeoutAt = DateTime.UtcNow.Add(timeout);
            var retryDelay = TimeSpan.FromMilliseconds(500);

            await Task.Delay(retryDelay).ConfigureAwait(false);

            using var connection = new SqlConnection(_dbConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            while (DateTime.UtcNow < timeoutAt)
            {
                var receivedIntegrationEvent = await connection.QuerySingleOrDefaultAsync(
                        "SELECT * FROM [ReceivedIntegrationEvents] WHERE Id = @EventId AND EventType = @EventType",
                        new
                        {
                            EventId = integrationEvent.MessageId,
                            EventType = eventName,
                        })
                    .ConfigureAwait(false);

                if (receivedIntegrationEvent != null)
                    break;

                await Task.Delay(retryDelay).ConfigureAwait(false);
            }
        }
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
}
