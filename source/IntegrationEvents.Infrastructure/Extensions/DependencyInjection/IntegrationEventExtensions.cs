﻿// Copyright 2020 Energinet DataHub A/S
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

using Azure.Storage.Blobs;
using Energinet.DataHub.Core.App.Common.Identity;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.Core.Messaging.Communication.Extensions.DependencyInjection;
using Energinet.DataHub.Core.Messaging.Communication.Extensions.Options;
using Energinet.DataHub.Core.Messaging.Communication.Subscriber;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.DataAccess.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.IntegrationEvents.Application;
using Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.EventProcessors;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Contracts;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Extensions.DependencyInjection;

public static class IntegrationEventExtensions
{
    public static IServiceCollection AddIntegrationEventModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddTransient<IIntegrationEventProcessor, ActorActivatedIntegrationEventProcessor>()
            .AddTransient<IIntegrationEventProcessor, GridAreaOwnershipAssignedIntegrationEventProcessor>()
            .AddTransient<IIntegrationEventProcessor, ActorCertificateCredentialsRemovedEventProcessor>()
            .AddTransient<IIntegrationEventProcessor, ActorCertificateCredentialsAssignedEventProcessor>()
            .AddTransient<IIntegrationEventProcessor, ProcessDelegationConfiguredEventProcessor>()
            .AddTransient<IReadOnlyDictionary<string, IIntegrationEventProcessor>>(
                sp => sp.GetServices<IIntegrationEventProcessor>()
                    .ToDictionary(m => m.EventTypeToHandle, m => m));

        services.AddSubscriber<IntegrationEventHandler>(
        [
            ActorActivated.Descriptor,
            GridAreaOwnershipAssigned.Descriptor,
            ActorCertificateCredentialsRemoved.Descriptor,
            ActorCertificateCredentialsAssigned.Descriptor,
            ProcessDelegationConfigured.Descriptor,
        ]);
        // Dead-letter logging
        services.AddDeadLetterHandlerForIsolatedWorker(
            configuration,
            tokenCredentialFactory: sp => sp.GetRequiredService<TokenCredentialProvider>().Credential);
        services
            .AddHealthChecks()
            .AddAzureBlobStorage(
                clientFactory: sp =>
                {
                    var options = sp.GetRequiredService<IOptions<BlobDeadLetterLoggerOptions>>();
                    var clientFactory = sp.GetRequiredService<IAzureClientFactory<BlobServiceClient>>();
                    return clientFactory.CreateClient(options.Value.ContainerName);
                },
                name: "dead-letter-logging");

        services
            .AddTransient<IDataRetention, ReceivedIntegrationEventsRetention>()
            .AddTransient<IReceivedIntegrationEventRepository, ReceivedIntegrationEventRepository>()
            .AddTransient<IIntegrationEventHandler, IntegrationEventHandler>();

        services.AddOptions<IntegrationEventsOptions>()
            .BindConfiguration(IntegrationEventsOptions.SectionName)
            .ValidateDataAnnotations();

        services.AddDapperConnectionToDatabase(configuration);

        // Durable Task
        services.AddDurableClientFactory(options =>
        {
            options.ConnectionName = "OrchestrationsStorageConnectionString";
            options.TaskHub = configuration["OrchestrationsTaskHubName"]!;
            options.IsExternalClient = true;
        });

        services
            .AddIntegrationEventsHealthChecks()
            .AddFeatureFlags();

        return services;
    }
}
