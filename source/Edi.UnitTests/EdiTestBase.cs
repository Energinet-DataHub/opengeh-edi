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

using BuildingBlocks.Application.Extensions.DependencyInjection;
using Energinet.DataHub.BuildingBlocks.Tests;
using Energinet.DataHub.BuildingBlocks.Tests.Logging;
using Energinet.DataHub.Core.App.Common.Extensions.DependencyInjection;
using Energinet.DataHub.Core.Messaging.Communication.Extensions.Options;
using Energinet.DataHub.EDI.DataAccess.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.DataAccess.UnitOfWork.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.MasterData.Application.Extensions.DependencyInjection;
using Energinet.DataHub.Wholesale.Edi.Extensions.DependencyInjection;
using Energinet.DataHub.Wholesale.Edi.Validation;
using Energinet.DataHub.Wholesale.Edi.Validation.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Energinet.DataHub.Wholesale.Edi.UnitTests;

public class EdiTestBase
{
    private readonly EdiFixture _ediFixture;
    private readonly ITestOutputHelper _testOutputHelper;

    public EdiTestBase(EdiFixture ediFixture, ITestOutputHelper testOutputHelper)
    {
        _ediFixture = ediFixture;
        _testOutputHelper = testOutputHelper;
        ediFixture.DatabaseManager.CleanupDatabase();
    }

    public ServiceProvider Services { get; set; } = null!;

    public void SetupServiceCollection()
    {
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DB_CONNECTION_STRING"] = _ediFixture.DatabaseManager.ConnectionString,
            [$"{ServiceBusNamespaceOptions.SectionName}:{nameof(ServiceBusNamespaceOptions.FullyQualifiedNamespace)}"] = "Fake",
        });

        var services = new ServiceCollection();
        var configuration = builder.Build();

        services
            .AddWholesaleServicesRequestValidation()
            .AddAggregatedTimeSeriesRequestValidation()
            .AddNodaTimeForApplication()
            .AddBuildingBlocks(configuration)
            .AddDapperConnectionToDatabase(configuration)
            .AddMasterDataModule(configuration)
            .AddDataAccessUnitOfWorkModule()
            .AddSerializer()
            .AddTestLogger(_testOutputHelper)
            .AddJavaScriptEncoder();

        services.AddScoped<IConfiguration>(_ => configuration);
        services.AddTransient<PeriodValidationHelper>();

        Services = services.BuildServiceProvider();
    }
}
