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

using System.Diagnostics;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;

namespace Communication.Internal;

/// <summary>
/// The sender runs as a background service
/// </summary>
public class OutboxSender : IOutboxSender
{
    private readonly IPointToPointEventProvider _pointToPointEventProvider;
    private readonly IServiceBusQueueSenderProvider _serviceBusQueueSenderProvider;
    private readonly IServiceBusQueueMessageFactory _serviceBusQueueMessageFactory;
    private readonly ILogger _logger;

    public OutboxSender(
        IPointToPointEventProvider pointToPointEventProvider,
        IServiceBusQueueSenderProvider serviceBusQueueSenderProvider,
        IServiceBusQueueMessageFactory serviceBusQueueMessageFactory,
        ILogger<OutboxSender> logger)
    {
        _pointToPointEventProvider = pointToPointEventProvider;
        _serviceBusQueueSenderProvider = serviceBusQueueSenderProvider;
        _serviceBusQueueMessageFactory = serviceBusQueueMessageFactory;
        _logger = logger;
    }

    public async Task SendAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var eventCount = 0;
        var messageBatch = await _serviceBusQueueSenderProvider.Instance.CreateMessageBatchAsync(cancellationToken).ConfigureAwait(false);

        await foreach (var @event in _pointToPointEventProvider.GetAsync().ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            eventCount++;
            var serviceBusMessage = _serviceBusQueueMessageFactory.Create(@event);
            if (!messageBatch.TryAddMessage(serviceBusMessage))
            {
                await SendBatchAsync(messageBatch).ConfigureAwait(false);
                messageBatch = await _serviceBusQueueSenderProvider.Instance.CreateMessageBatchAsync(cancellationToken).ConfigureAwait(false);

                if (!messageBatch.TryAddMessage(serviceBusMessage))
                {
                    await SendMessageThatExceedsBatchLimitAsync(serviceBusMessage).ConfigureAwait(false);
                }
            }
        }

        try
        {
            await _serviceBusQueueSenderProvider.Instance.SendMessagesAsync(messageBatch, cancellationToken).ConfigureAwait(false);
        }
#pragma warning disable CA1031
        catch (Exception e)
#pragma warning restore CA1031
        {
            _logger.LogError(e, "Failed to send messages from outbox");
        }

        if (eventCount > 0)
            _logger.LogDebug("Sent {EventCount} integration events in {Time} ms", eventCount, stopwatch.Elapsed.TotalMilliseconds);
    }

    private async Task SendBatchAsync(ServiceBusMessageBatch batch)
    {
        await _serviceBusQueueSenderProvider.Instance.SendMessagesAsync(batch).ConfigureAwait(false);
    }

    private async Task SendMessageThatExceedsBatchLimitAsync(ServiceBusMessage serviceBusMessage)
    {
        await _serviceBusQueueSenderProvider.Instance.SendMessageAsync(serviceBusMessage).ConfigureAwait(false);
    }
}
