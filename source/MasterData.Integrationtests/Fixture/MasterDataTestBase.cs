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
using Energinet.DataHub.Core.Messaging.Communication.Extensions.Options;
using Energinet.DataHub.EDI.IntegrationTests.TestDoubles;
using Energinet.DataHub.EDI.MasterData.Application.Extensions.DependencyInjection;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.MasterData.IntegrationTests.Fixture;

public class MasterDataTestBase
{
    private readonly MasterDataFixture _masterDataFixture;
    private readonly ITestOutputHelper _testOutputHelper;

    public MasterDataTestBase(MasterDataFixture masterDataFixture, ITestOutputHelper testOutputHelper)
    {
        _masterDataFixture = masterDataFixture;
        _testOutputHelper = testOutputHelper;
        CleanupDatabase();
    }

    public ServiceProvider Services { get; set; } = null!;

    public void SetupServiceCollection()
    {
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DB_CONNECTION_STRING"] = _masterDataFixture.DatabaseManager.ConnectionString,
            [$"{ServiceBusNamespaceOptions.SectionName}:{nameof(ServiceBusNamespaceOptions.FullyQualifiedNamespace)}"] = "Fake",
            [$"{BlobDeadLetterLoggerOptions.SectionName}:{nameof(BlobDeadLetterLoggerOptions.StorageAccountUrl)}"] = "https://fakeurl.com",
            [$"{BlobDeadLetterLoggerOptions.SectionName}:{nameof(BlobDeadLetterLoggerOptions.ContainerName)}"] = "fake-container-name",
        });

        var services = new ServiceCollection();
        var configuration = builder.Build();

        services
            .AddNodaTimeForApplication()
            .AddMasterDataModule(configuration);

        services.AddScoped<IConfiguration>(_ => configuration);

        // Add test logger
        services.AddSingleton(sp => _testOutputHelper);
        services.Add(ServiceDescriptor.Singleton(typeof(Logger<>), typeof(Logger<>)));
        services.Add(ServiceDescriptor.Transient(typeof(ILogger<>), typeof(TestLogger<>)));

        Services = services.BuildServiceProvider();
    }

    public void CleanupDatabase()
    {
     var cleanupStatement =
                 $"DELETE FROM [dbo].[MessageRegistry] " +
                 $"DELETE FROM [dbo].[TransactionRegistry]" +
                 $"DELETE FROM [dbo].[OutgoingMessages] " +
                 $"DELETE FROM [dbo].[QueuedInternalCommands] " +
                 $"DELETE FROM [dbo].[MarketEvaluationPoints]" +
                 $"DELETE FROM [dbo].[Actor]" +
                 $"DELETE FROM [dbo].[ReceivedIntegrationEvents]" +
                 $"DELETE FROM [dbo].[AggregatedMeasureDataProcessGridAreas]" +
                 $"DELETE FROM [dbo].[AggregatedMeasureDataProcesses]" +
                 $"DELETE FROM [dbo].[ArchivedMessages]" +
                 $"DELETE FROM [dbo].[MarketDocuments]" +
                 $"DELETE FROM [dbo].[Bundles]" +
                 $"DELETE FROM [dbo].[ActorMessageQueues]" +
                 $"DELETE FROM [dbo].[ReceivedInboxEvents]" +
                 $"DELETE FROM [dbo].[MessageRegistry]" +
                 $"DELETE FROM [dbo].[TransactionRegistry]" +
                 $"DELETE FROM [dbo].[GridAreaOwner]" +
                 $"DELETE FROM [dbo].[ActorCertificate]" +
                 $"DELETE FROM [dbo].[WholesaleServicesProcessChargeTypes]" +
                 $"DELETE FROM [dbo].[WholesaleServicesProcessGridAreas]" +
                 $"DELETE FROM [dbo].[WholesaleServicesProcesses]" +
                 $"DELETE FROM [dbo].[ProcessDelegation]";
     
        using var connection = new SqlConnection(_masterDataFixture.DatabaseManager.ConnectionString);
        connection.Open();

        using (var command = new SqlCommand(cleanupStatement, connection))
        {
            command.ExecuteNonQuery();
        }

        connection.Close();
    }
}
