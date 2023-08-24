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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.Core.Messaging.Communication.Internal.Subscriber;

internal sealed class IntegrationEventSubscriber : IIntegrationEventSubscriber
{
    private readonly IOptions<SubscriberWorkerOptions> _options;
    private readonly IServiceBusProcessorFactory _serviceBusProcessorFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IntegrationEventSubscriber> _logger;
    private ServiceBusProcessor? _processor;

    public IntegrationEventSubscriber(
        IOptions<SubscriberWorkerOptions> options,
        IServiceBusProcessorFactory serviceBusProcessorFactory,
        IServiceProvider serviceProvider,
        ILogger<IntegrationEventSubscriber> logger)
    {
        _options = options;
        _serviceBusProcessorFactory = serviceBusProcessorFactory;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_processor is not null)
        {
            throw new InvalidOperationException($"This {nameof(IIntegrationEventSubscriber)} is already running");
        }

        _processor = _serviceBusProcessorFactory.CreateProcessor(_options.Value.TopicName, _options.Value.SubscriptionName);

        _processor.ProcessMessageAsync += OnMessageReceivedAsync;
        _processor.ProcessErrorAsync += OnMessageErrorAsync;

        await _processor.StartProcessingAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor is null)
        {
            return;
        }

        await _processor.StopProcessingAsync(cancellationToken).ConfigureAwait(false);

        _processor.ProcessMessageAsync -= OnMessageReceivedAsync;
        _processor.ProcessErrorAsync -= OnMessageErrorAsync;

        await _processor.DisposeAsync().ConfigureAwait(false);
        _processor = null;
    }

    private async Task OnMessageReceivedAsync(ProcessMessageEventArgs args)
    {
        var integrationEventServiceBusMessage = new IntegrationEventServiceBusMessage(
            Guid.Parse(args.Message.MessageId),
            args.Message.Subject,
            args.Message.ApplicationProperties,
            new BinaryData(args.Message.Body.ToArray()));

        using (var scope = _serviceProvider.CreateScope())
        {
            var subscriber = scope.ServiceProvider.GetRequiredService<ISubscriber>();
            await subscriber.HandleAsync(integrationEventServiceBusMessage).ConfigureAwait(false);
        }

        await args.CompleteMessageAsync(args.Message, args.CancellationToken).ConfigureAwait(false);
    }

    private Task OnMessageErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Failed to process message {MessageId}", args.Identifier);
        return Task.CompletedTask;
    }
}
