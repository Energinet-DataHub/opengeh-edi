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

using Energinet.DataHub.Core.App.Common.Extensions.DependencyInjection;
using Energinet.DataHub.Core.App.Common.Identity;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Extensions.DependencyInjection;

public static class FileStorageExtensions
{
    private const string HealthCheckName = "EDI blob file storage";

    /// <summary>
    /// Register services and health checks for file storage.
    /// </summary>
    /// <remarks>
    /// Expects "AddTokenCredentialProvider" has been called to register <see cref="TokenCredentialProvider"/>.
    /// </remarks>
    public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<BlobServiceClientConnectionOptions>()
            .BindConfiguration(BlobServiceClientConnectionOptions.SectionName)
            .ValidateDataAnnotations();

        var blobServiceClientConnectionOptions =
            configuration
                .GetSection(BlobServiceClientConnectionOptions.SectionName)
                .Get<BlobServiceClientConnectionOptions>()
            ?? throw new InvalidOperationException("Missing Blob Service Client Connection configuration.");

        services
            .AddAzureClients(builder =>
            {
                builder.UseCredential(sp => sp.GetRequiredService<TokenCredentialProvider>().Credential);

                builder
                    .AddBlobServiceClient(new Uri(blobServiceClientConnectionOptions.StorageAccountUrl))
                    .WithName(blobServiceClientConnectionOptions.ClientName);
            });

        services.TryAddBlobStorageHealthCheck(
            HealthCheckName,
            blobServiceClientConnectionOptions.ClientName);

        services.AddTransient<IFileStorageClient, BlobFileStorageClient>();

        return services;
    }
}
