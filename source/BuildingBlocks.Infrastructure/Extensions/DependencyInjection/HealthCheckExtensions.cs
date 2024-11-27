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
using Azure.Storage.Blobs;
using Energinet.DataHub.Core.App.Common.Diagnostics.HealthChecks;
using Energinet.DataHub.Core.App.Common.Extensions.DependencyInjection;
using Energinet.DataHub.Core.Messaging.Communication.Extensions.Builder;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Extensions.DependencyInjection;

public static class HealthCheckExtensions
{
    /// <summary>
    /// Used for Service Bus queues where the app have peek (receiver) permissions
    /// </summary>
    public static IServiceCollection TryAddExternalDomainServiceBusQueuesHealthCheck(
        this IServiceCollection services,
        string serviceBusFullyQualifiedNamespace,
        params string[] queueNames)
    {
        ArgumentNullException.ThrowIfNull(serviceBusFullyQualifiedNamespace);
        ArgumentNullException.ThrowIfNull(queueNames);

        foreach (var name in queueNames)
        {
            services.TryAddHealthChecks(
                registrationKey: name,
                (key, builder) =>
                {
                    var defaultAzureCredential = new DefaultAzureCredential();

                    builder.AddAzureServiceBusQueue(
                        name: key,
                        fullyQualifiedNamespace: serviceBusFullyQualifiedNamespace,
                        queueName: key,
                        tokenCredential: defaultAzureCredential);

                    builder.AddServiceBusQueueDeadLetter(
                        fullyQualifiedNamespaceFactory: _ => serviceBusFullyQualifiedNamespace,
                        queueNameFactory: _ => key,
                        tokenCredentialFactory: _ => defaultAzureCredential,
                        $"Dead-letter ({key})",
                        [HealthChecksConstants.StatusHealthCheckTag]);
                });
        }

        return services;
    }

    public static IServiceCollection TryAddBlobStorageHealthCheck(
        this IServiceCollection services,
        string name,
        string blobClientName)
    {
        services.TryAddHealthChecks(
            name,
            (key, builder) =>
            {
                builder.AddAzureBlobStorage(
                    clientFactory: sp =>
                        sp.GetRequiredService<IAzureClientFactory<BlobServiceClient>>().CreateClient(blobClientName),
                    name: key);
            });

        return services;
    }
}
