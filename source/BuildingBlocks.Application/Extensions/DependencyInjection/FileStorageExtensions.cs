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
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FileStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Application.Extensions.DependencyInjection;

public static class FileStorageExtensions
{
    private const string FileStorageName = "edi-documents-storage";

    public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
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

        var uri = configuration["AZURE_STORAGE_ACCOUNT_URL"];

        // If this uri is null, then we are running our solution locally or running tests.
        // For our tests we will have a call of "AddFileStorage" for every test method. Hence "new uri(xxx)" will be called for every test.
        // Which will slow down the tests. Which we can remove by having this check.
        var isIntegrationTest = uri == null;
        if (!isIntegrationTest)
        {
            services.TryAddBlobStorageHealthCheck(
                FileStorageName,
                new Uri(uri!));
        }

        return services;
    }
}
