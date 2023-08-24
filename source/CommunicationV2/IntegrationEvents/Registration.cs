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

using Energinet.DataHub.Core.App.WebApp.Diagnostics.HealthChecks;
using Energinet.DataHub.Core.Messaging.Communication.Internal.Publisher;
using Energinet.DataHub.Core.Messaging.Communication.Internal.Subscriber;
using Energinet.DataHub.Core.Messaging.Communication.Publisher;
using Energinet.DataHub.Core.Messaging.Communication.Subscriber;
using Google.Protobuf.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.Core.Messaging.Communication;

public static class Registration
{
    /// <summary>
    /// Method for registering publisher.
    /// It is the responsibility of the caller to register the dependencies of the <see cref="IIntegrationEventProvider"/> implementation.
    /// </summary>
    /// <typeparam name="TIntegrationEventProvider">The type of the service to use for outbound events.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddPublisher<TIntegrationEventProvider>(this IServiceCollection services)
        where TIntegrationEventProvider : class, IIntegrationEventProvider
    {
        services.AddSingleton<IServiceBusSenderProvider, ServiceBusSenderProvider>();
        services.AddScoped<IIntegrationEventProvider, TIntegrationEventProvider>();
        services.AddScoped<IPublisher, Internal.Publisher.Publisher>();
        services.AddScoped<IServiceBusMessageFactory, ServiceBusMessageFactory>();
        return services;
    }

    /// <summary>
    /// Method for registering publisher worker.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddPublisherWorker(this IServiceCollection services)
    {
        services.AddHostedService<PublisherTrigger>();

        services
            .AddHealthChecks()
            .AddRepeatingTriggerHealthCheck<PublisherTrigger>(TimeSpan.FromMinutes(1));

        return services;
    }

    /// <summary>
    /// Method for registering subscriber.
    /// It is the responsibility of the caller to register the dependencies of the <see cref="IIntegrationEventHandler"/> implementation.
    /// </summary>
    /// <typeparam name="TIntegrationEventHandler">The type of the service to use for outbound events.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <param name="messageDescriptors">List of known <see cref="MessageDescriptor"/></param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddSubscriber<TIntegrationEventHandler>(this IServiceCollection services, IEnumerable<MessageDescriptor> messageDescriptors)
        where TIntegrationEventHandler : class, IIntegrationEventHandler
    {
        services.AddScoped<IIntegrationEventHandler, TIntegrationEventHandler>();
        services.AddScoped<IIntegrationEventFactory>(_ => new IntegrationEventFactory(messageDescriptors.ToList()));
        services.AddScoped<ISubscriber, Internal.Subscriber.Subscriber>();
        return services;
    }

    /// <summary>
    /// Method for registering subscriber worker.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddSubscriberWorker(this IServiceCollection services)
    {
        services.AddSingleton<IServiceBusProcessorFactory, ServiceBusProcessorFactory>();
        services.AddSingleton<IIntegrationEventSubscriber, IntegrationEventSubscriber>();
        services.AddHostedService<SubscriberTrigger>();
        return services;
    }
}
