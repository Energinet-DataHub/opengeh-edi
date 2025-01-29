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
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.FeatureFlag;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.Logging;
using Energinet.DataHub.EDI.BuildingBlocks.Tests.TestDoubles;
using Energinet.DataHub.EDI.IntegrationEvents.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.MasterData.Infrastructure.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.DurableTask.ContextImplementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Energinet.DataHub.EDI.IntegrationEvents.IntegrationTests.Fixture;

public class IntegrationEventsTestBase : IAsyncLifetime
{
    private readonly IntegrationEventsFixture _integrationEventsFixture;
    private readonly ITestOutputHelper _testOutputHelper;

    public IntegrationEventsTestBase(IntegrationEventsFixture integrationEventsFixture, ITestOutputHelper testOutputHelper)
    {
        _integrationEventsFixture = integrationEventsFixture;
        _testOutputHelper = testOutputHelper;
        FeatureFlagManagerStub = new();
    }

    protected ServiceProvider Services { get; private set; } = null!;

    protected FeatureFlagManagerStub FeatureFlagManagerStub { get; }

    public void SetupServiceCollection()
    {
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DB_CONNECTION_STRING"] = _integrationEventsFixture.DatabaseManager.ConnectionString,
            [$"{ServiceBusNamespaceOptions.SectionName}:{nameof(ServiceBusNamespaceOptions.FullyQualifiedNamespace)}"] = "Fake",
            [$"{BlobDeadLetterLoggerOptions.SectionName}:{nameof(BlobDeadLetterLoggerOptions.StorageAccountUrl)}"] = "https://fakeurl.com",
        });

        var services = new ServiceCollection();
        var configuration = builder.Build();

        services
            .AddNodaTimeForApplication()
            .AddMasterDataModule(configuration)
            .AddIntegrationEventModule(configuration)
            .AddTransient<IFeatureFlagManager>(_ => FeatureFlagManagerStub)
            .AddScoped<IDurableClient, DurableClientStub>()
            .AddScoped<IDurableClientFactory, DurableClientFactoryStub>();

        services.AddScoped<IConfiguration>(_ => configuration);

        // Add test logger
        services.AddTestLogger(_testOutputHelper);

        Services = services.BuildServiceProvider();
    }

    public Task InitializeAsync()
    {
        _integrationEventsFixture.DatabaseManager.CleanupDatabase();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
