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

using Communication.Internal;
using Energinet.DataHub.Core.App.WebApp.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;

namespace Communication;

public static class Registration
{
    /// <summary>
    /// Method for registering the communication library.
    /// It is the responsibility of the caller to register the dependencies of the
    /// <see cref="IPointToPointEventProvider"/> implementation.
    /// </summary>
    public static IServiceCollection AddCommunication<TPointToPointEventProvider>(
        this IServiceCollection services,
        string serviceBusPointToPointEventWriteConnectionString,
        string pointToPointEventTopicName)
        where TPointToPointEventProvider : class, IPointToPointEventProvider
    {
        services.AddScoped<IPointToPointEventProvider, TPointToPointEventProvider>();
        services.AddSingleton<IServiceBusQueueSenderProvider>(
            _ => new ServiceBusQueueSenderProvider(serviceBusPointToPointEventWriteConnectionString, pointToPointEventTopicName));
        services.AddScoped<IOutboxSender, OutboxSender>();
        services.AddScoped<IServiceBusQueueMessageFactory, ServiceBusQueueMessageFactory>();

        RegisterHostedServices(services);

        return services;
    }

    private static void RegisterHostedServices(IServiceCollection services)
    {
        services.AddHostedService<OutboxSenderTrigger>();

        services
            .AddHealthChecks()
            .AddRepeatingTriggerHealthCheck<OutboxSenderTrigger>(TimeSpan.FromMinutes(1));
    }
}
