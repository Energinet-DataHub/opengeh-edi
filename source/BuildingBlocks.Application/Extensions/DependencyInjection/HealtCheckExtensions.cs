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

using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BuildingBlocks.Application.Extensions.DependencyInjection;

public static class HealtCheckExtensions
{
    /// <summary>
    /// Used for Service Bus queues where the app have peek (receiver) permissions
    /// </summary>
    public static IServiceCollection TryAddExternalDomainServiceBusQueuesHealthCheck(this IServiceCollection services, string serviceBusConnectionString, params string[] queueNames)
    {
        ArgumentNullException.ThrowIfNull(serviceBusConnectionString);
        ArgumentNullException.ThrowIfNull(queueNames);
        foreach (var name in queueNames)
        {
            if (QueueHealthCheckIsAdded(services, name))
            {
                return services;
            }

            services.AddHealthChecks()
                .AddAzureServiceBusQueue(
                    name: name + "Exists",
                    connectionString: serviceBusConnectionString,
                    queueName: name);
            services.TryAddSingleton(new ServiceBusQueueHealthCheckIsAdded(name));
        }

        return services;
    }

    public static IServiceCollection AddBlobStorageHealthCheck(this IServiceCollection services, string name, string blobConnectionString)
    {
        services.AddHealthChecks().AddAzureBlobStorage(blobConnectionString, name: name);
        return services;
    }

    public static IServiceCollection AddBlobStorageHealthCheck(this IServiceCollection services, string name, Uri storageAccountUri)
    {
        services.AddHealthChecks().AddAzureBlobStorage(storageAccountUri, new DefaultAzureCredential(), name: name);

        return services;
    }

    private static bool QueueHealthCheckIsAdded(IServiceCollection services, string name)
    {
        return services.Any(
            service =>
                service.ServiceType == typeof(ServiceBusQueueHealthCheckIsAdded)
                && service.ImplementationInstance is ServiceBusQueueHealthCheckIsAdded
                && ((ServiceBusQueueHealthCheckIsAdded)service.ImplementationInstance).Name == name);
    }

    private sealed record ServiceBusQueueHealthCheckIsAdded(string Name);
}
