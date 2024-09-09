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
using Azure.Messaging.ServiceBus;
using BuildingBlocks.Application.Extensions.Options;
using Energinet.DataHub.EDI.Process.Domain.Wholesale;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.Process.Infrastructure.Wholesale;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.EDI.Process.Application.Extensions.DependencyInjection;

public static class WholesaleInboxExtensions
{
    public static IServiceCollection AddWholesaleInbox(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IWholesaleInboxClient, WholesaleInboxClient>();

        services
            .AddOptions<WholesaleInboxQueueOptions>()
            .BindConfiguration(WholesaleInboxQueueOptions.SectionName)
            .ValidateDataAnnotations();

        var wholesaleInboxQueueOptions =
            configuration
                .GetRequiredSection(WholesaleInboxQueueOptions.SectionName)
                .Get<WholesaleInboxQueueOptions>()
            ?? throw new InvalidOperationException("Missing Wholesale Inbox configuration.");

        services.AddAzureClients(builder =>
        {
            builder
                .AddClient<ServiceBusSender, ServiceBusClientOptions>((_, _, provider) =>
                    provider
                        .GetRequiredService<ServiceBusClient>()
                        .CreateSender(wholesaleInboxQueueOptions.QueueName))
                .WithName(wholesaleInboxQueueOptions.QueueName);
        });

        // Health checks
        services.AddHealthChecks()
            .AddAzureServiceBusQueue(
                sp => sp.GetRequiredService<IOptions<ServiceBusNamespaceOptions>>().Value.FullyQualifiedNamespace,
                sp => sp.GetRequiredService<IOptions<WholesaleInboxQueueOptions>>().Value.QueueName,
                _ => new DefaultAzureCredential(),
                name: wholesaleInboxQueueOptions.QueueName);

        return services;
    }
}
