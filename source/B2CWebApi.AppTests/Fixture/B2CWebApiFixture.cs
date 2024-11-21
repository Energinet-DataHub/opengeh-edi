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

using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Blobs;
using Energinet.DataHub.BuildingBlocks.Tests.Database;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Azurite;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Configuration;
using Energinet.DataHub.Core.FunctionApp.TestCommon.OpenIdJwt;
using Energinet.DataHub.Core.FunctionApp.TestCommon.ServiceBus.ResourceProvider;
using Energinet.DataHub.Core.Messaging.Communication.Extensions.Options;
using Energinet.DataHub.Core.TestCommon.Diagnostics;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.B2CWebApi.AppTests.Fixture;

public class B2CWebApiFixture : IAsyncLifetime
{
    private const string IncomingMessagesQueueName = "incoming-messages-queue";

    public B2CWebApiFixture()
    {
        TestLogger = new TestDiagnosticsLogger();
        IntegrationTestConfiguration = new IntegrationTestConfiguration();
        DatabaseManager = new EdiDatabaseManager("B2CWebApi");
        AzuriteManager = new AzuriteManager(useOAuth: true);
        OpenIdJwtManager = new OpenIdJwtManager(IntegrationTestConfiguration.B2CSettings);
        ServiceBusResourceProvider = new ServiceBusResourceProvider(
            TestLogger,
            IntegrationTestConfiguration.ServiceBusFullyQualifiedNamespace,
            IntegrationTestConfiguration.Credential);

        B2CWebApiApplicationFactory = new B2CWebApiApplicationFactory();
    }

    public EdiDatabaseManager DatabaseManager { get; }

    public OpenIdJwtManager OpenIdJwtManager { get; }

    [NotNull]
    public HttpClient? WebApiClient { get; private set; }

    public TestDiagnosticsLogger TestLogger { get; }

    private ServiceBusResourceProvider ServiceBusResourceProvider { get; }

    private AzuriteManager AzuriteManager { get; }

    private IntegrationTestConfiguration IntegrationTestConfiguration { get; }

    private B2CWebApiApplicationFactory B2CWebApiApplicationFactory { get; }

    public async Task InitializeAsync()
    {
        AzuriteManager.StartAzurite();
        CreateRequiredContainers();

        OpenIdJwtManager.StartServer();

        await DatabaseManager.CreateDatabaseAsync();

        var incomingMessagesQueue = await ServiceBusResourceProvider.BuildQueue(IncomingMessagesQueueName)
            .CreateAsync();

        var appSettings = GetWebApiAppSettings(incomingMessagesQueue.Name);

        B2CWebApiApplicationFactory.AppSettings = appSettings;

        // Create the web api client (which also starts the web api application)
        WebApiClient = B2CWebApiApplicationFactory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        AzuriteManager.Dispose();
        OpenIdJwtManager.Dispose();
        await DatabaseManager.DeleteDatabaseAsync();
        await ServiceBusResourceProvider.DisposeAsync();
        await B2CWebApiApplicationFactory.DisposeAsync();
        WebApiClient.Dispose();
    }

    /// <summary>
    /// Set test output helper to enable logging to xUnit test output.
    /// <remarks>
    /// Should be set in a test's constructor (by injecting <see cref="ITestOutputHelper"/>) and MUST then be set back
    /// to null when disposing the test.
    /// </remarks>
    /// </summary>
    public void SetTestOutputHelper(ITestOutputHelper? testOutputHelper)
    {
        TestLogger.TestOutputHelper = testOutputHelper;
    }

    private Dictionary<string, string?> GetWebApiAppSettings(string incomingMessagesQueueName)
    {
        var dbConnectionString = DatabaseManager.ConnectionString;
        if (!dbConnectionString.Contains("Trust")) // Trust Server Certificate might be required for some
            dbConnectionString = $"{dbConnectionString};Trust Server Certificate=True;";

        var appSettings = new Dictionary<string, string?>
        {
            { "DB_CONNECTION_STRING", dbConnectionString },
            { "AZURE_STORAGE_ACCOUNT_URL", AzuriteManager.BlobStorageServiceUri.AbsoluteUri },
            { "UserAuthentication:MitIdExternalMetadataAddress", "YourMitIdExternalMetadataAddress" },
            { "UserAuthentication:ExternalMetadataAddress", OpenIdJwtManager.ExternalMetadataAddress },
            { "UserAuthentication:InternalMetadataAddress", OpenIdJwtManager.InternalMetadataAddress },
            { "UserAuthentication:BackendBffAppId", OpenIdJwtManager.TestBffAppId },
            { $"{ServiceBusNamespaceOptions.SectionName}:{nameof(ServiceBusNamespaceOptions.FullyQualifiedNamespace)}", ServiceBusResourceProvider.FullyQualifiedNamespace },
            { "IncomingMessages:QueueName", incomingMessagesQueueName },
            { "OrchestrationsStorageAccountConnectionString", AzuriteManager.FullConnectionString },
            { "OrchestrationsTaskHubName", "EdiTest01" },
        };

        return appSettings;
    }

    private void CreateRequiredContainers()
    {
        List<FileStorageCategory> containerCategories = [
            FileStorageCategory.ArchivedMessage(),
            FileStorageCategory.OutgoingMessage(),
        ];

        var blobServiceClient = new BlobServiceClient(AzuriteManager.BlobStorageConnectionString);
        foreach (var fileStorageCategory in containerCategories)
        {
            var container = blobServiceClient.GetBlobContainerClient(fileStorageCategory.Value);
            var containerExists = container.Exists();

            if (!containerExists)
                container.Create();
        }
    }
}
