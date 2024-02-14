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
using Azure.Storage.Blobs;
using BuildingBlocks.Application.FeatureFlag;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.MessageBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using EdiServiceBusClientOptions = BuildingBlocks.Application.Configuration.Options.ServiceBusClientOptions;

namespace BuildingBlocks.Application.Configuration;

public static class BuildingBlockConfiguration
{
    public static void AddBuildingBlocks(this IServiceCollection services, IConfiguration configuration)
    {
        AddServiceBus(services, configuration);
        AddDatabase(services, configuration);
        AddFileStorage(services, configuration);
        AddFeatureFlags(services);
    }

    private static void AddServiceBus(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<EdiServiceBusClientOptions>()
            .Bind(configuration)
            .Validate(o => !string.IsNullOrEmpty(o.SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_SEND), "SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_SEND must be set");

        services.AddSingleton<ServiceBusClient>(provider => new ServiceBusClient(
            provider.GetRequiredService<IOptions<EdiServiceBusClientOptions>>().Value.SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_SEND,
            new ServiceBusClientOptions()
            {
                TransportType = ServiceBusTransportType.AmqpWebSockets,
            }));

        services.AddSingleton<IServiceBusSenderFactory, ServiceBusSenderFactory>();
    }

    private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<SqlDatabaseConnectionOptions>()
            .Bind(configuration)
            .Validate(o => !string.IsNullOrEmpty(o.DB_CONNECTION_STRING), "DB_CONNECTION_STRING must be set");

        services.AddSingleton<IDatabaseConnectionFactory, SqlDatabaseConnectionFactory>();
    }

    private static void AddFileStorage(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<BlobServiceClientConnectionOptions>()
            .Bind(configuration)
            .Validate(o => !string.IsNullOrEmpty(o.AZURE_STORAGE_ACCOUNT_CONNECTION_STRING) || !string.IsNullOrEmpty(o.AZURE_STORAGE_ACCOUNT_URL), $"{nameof(BlobServiceClientConnectionOptions.AZURE_STORAGE_ACCOUNT_CONNECTION_STRING)} or {nameof(BlobServiceClientConnectionOptions.AZURE_STORAGE_ACCOUNT_URL)} (if using Default Azure Credentials) must be set in configuration");

        services.AddTransient<BlobServiceClient>(
            x =>
            {
                var options = x.GetRequiredService<IOptions<BlobServiceClientConnectionOptions>>();
                var blobServiceClient = !string.IsNullOrEmpty(options.Value.AZURE_STORAGE_ACCOUNT_URL) // Uses AZURE_STORAGE_ACCOUNT_URL to run with Azure credentials in our Azure environments, and uses AZURE_STORAGE_ACCOUNT_CONNECTION_STRING to run locally and in our tests
                    ? new BlobServiceClient(new Uri(options.Value.AZURE_STORAGE_ACCOUNT_URL), new DefaultAzureCredential())
                    : new BlobServiceClient(options.Value.AZURE_STORAGE_ACCOUNT_CONNECTION_STRING);

                return blobServiceClient;
            });

        services.AddTransient<IFileStorageClient, DataLakeFileStorageClient>();
    }

    private static void AddFeatureFlags(IServiceCollection services)
    {
        services.AddFeatureManagement();
        services.AddScoped<IFeatureFlagManager, MicrosoftFeatureFlagManager>();
    }
}
