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
using Energinet.DataHub.Core.Outbox.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.B2BApi.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.Logging;
using Energinet.DataHub.EDI.DataAccess.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.DataAccess.UnitOfWork.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.MasterData.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.Outbox.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
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
        masterDataFixture.DatabaseManager.CleanupDatabase();
    }

    public ServiceProvider Services { get; set; } = null!;

    public void SetupServiceCollection()
    {
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DB_CONNECTION_STRING"] = _masterDataFixture.DatabaseManager.ConnectionString,
            [$"{BlobServiceClientConnectionOptions.SectionName}:{nameof(BlobServiceClientConnectionOptions.StorageAccountUrl)}"] =
                "https://fake.fakeurl.com",
            [$"{ServiceBusNamespaceOptions.SectionName}:{nameof(ServiceBusNamespaceOptions.FullyQualifiedNamespace)}"] = "Fake",
        });

        var services = new ServiceCollection();
        var configuration = builder.Build();

        services
            .AddB2BAuthentication(new()
            {
                ValidateAudience = false,
                ValidateLifetime = false,
                ValidateIssuer = false,
                SignatureValidator = (token, parameters) => new JsonWebToken(token),
            })
            .AddNodaTimeForApplication()
            .AddBuildingBlocks(configuration)
            .AddDapperConnectionToDatabase(configuration)
            .AddMasterDataModule(configuration)
            .AddDataAccessUnitOfWorkModule()
            .AddSerializer()
            .AddJavaScriptEncoder()
            .AddAuditLog()
            .AddOutboxContext(configuration)
            .AddOutboxClient<OutboxContext>()
            .AddOutboxProcessor<OutboxContext>();

        services.AddScoped<IConfiguration>(_ => configuration);

        // Add test logger
        services.AddTestLogger(_testOutputHelper);

        Services = services.BuildServiceProvider();
    }
}
