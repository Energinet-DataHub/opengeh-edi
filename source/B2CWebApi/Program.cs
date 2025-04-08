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

using System.Reflection;
using Asp.Versioning;
using Azure.Identity;
using Energinet.DataHub.Core.App.Common.Extensions.DependencyInjection;
using Energinet.DataHub.Core.App.WebApp.Extensions.Builder;
using Energinet.DataHub.Core.App.WebApp.Extensions.DependencyInjection;
using Energinet.DataHub.Core.Outbox.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.ArchivedMessages.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.B2CWebApi.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.B2CWebApi.Security;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Configuration;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.DataAccess.UnitOfWork.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.MasterData.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.Outbox.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

const string subsystemName = "EDI";

// Add Azure App Configuration
builder.Configuration.AddAzureAppConfiguration(options =>
{
    var appConfigEndpoint = builder.Configuration[AppConfiguration.AppConfigEndpoint]!;
    options.Connect(new Uri(appConfigEndpoint), new DefaultAzureCredential())
        .UseFeatureFlags(featureFlagOptions =>
        {
            featureFlagOptions.SetRefreshInterval(TimeSpan.FromSeconds(5));
        });
});

builder.Services
    // Swagger
    .AddSwaggerForWebApp(Assembly.GetExecutingAssembly(), "EDI B2C Web API", null)
    .AddApiVersioningForWebApp(new ApiVersion(1, 0))

    // Logging
    .AddApplicationInsightsForWebApp(subsystemName)

    // Health checks
    .AddHealthChecksForWebApp()

    // System timer
    .AddNodaTimeForApplication()

    // Durable client (orchestration)
    .AddDurableClient(builder.Configuration)

    .AddOutboxContext(builder.Configuration)
    .AddOutboxClient<OutboxContext>()

    // Modules
    .AddDataAccessUnitOfWorkModule()
    .AddIncomingMessagesModule(builder.Configuration)
    .AddArchivedMessagesModule(builder.Configuration)
    .AddMasterDataModule(builder.Configuration)
    .AddAuditLog()

    // Security
    // Note that this requires you to have
    // UserAuthentication__MitIdExternalMetadataAddress
    // UserAuthentication__ExternalMetadataAddress
    // UserAuthentication__InternalMetadataAddress
    // UserAuthentication__BackendBffAppId
    // Defined in app settings
    .AddJwtTokenSecurity(builder.Configuration)

    // Encoder
    .AddJavaScriptEncoder()

    // Serializer
    .AddSerializer()

    // Azure App Configuration
    .AddAzureAppConfiguration()

    // Http
    .AddHttpClient()
    .AddControllers();

// ***********************************************************************************************
// App building start here, aka Configure if one uses StartUp
// ***********************************************************************************************
var app = builder.Build();

var isDevelopment = app.Environment.IsDevelopment();

app.UseRouting();

if (isDevelopment)
    app.UseDeveloperExceptionPage();

app
    .UseSwaggerForWebApp()
    .UseHttpsRedirection()
    .UseAuthentication()
    .UseAuthorization()
    .UseUserMiddlewareForWebApp<FrontendUser>();

app.MapControllers().RequireAuthorization();

app.MapLiveHealthChecks();
app.MapReadyHealthChecks();
app.MapStatusHealthChecks();

app.Run();

// This is needed in order to test the dependency injection
namespace Energinet.DataHub.EDI.B2CWebApi
{
    public partial class Program
    {
    }
}
