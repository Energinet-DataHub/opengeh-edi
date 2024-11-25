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
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Application.Extensions.DependencyInjection;

public static class FileStorageExtensions
{
    private const string HealthCheckName = "EDI blob file storage";

    public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services
            .AddOptions<BlobServiceClientConnectionOptions>()
            .Bind(configuration);

        var blobServiceClientConnectionOptions =
            configuration
                .GetSection(BlobServiceClientConnectionOptions.SectionName)
                .Get<BlobServiceClientConnectionOptions>()
            ?? throw new InvalidOperationException("Missing Blob Service Client Connection configuration.");

        services.AddAzureClients(
            builder =>
            {
                builder.UseCredential(new DefaultAzureCredential());

                builder
                    .AddBlobServiceClient(new Uri(blobServiceClientConnectionOptions.StorageAccountUrl))
                    .WithName(blobServiceClientConnectionOptions.ClientName);
            });

        services.TryAddBlobStorageHealthCheck(
            HealthCheckName,
            blobServiceClientConnectionOptions.ClientName);

        services.AddTransient<IFileStorageClient, DataLakeFileStorageClient>();

        return services;
    }
}
