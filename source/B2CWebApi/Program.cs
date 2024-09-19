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

using System.Reflection;
using System.Text.Json.Serialization;
using Asp.Versioning;
using BuildingBlocks.Application.Extensions.DependencyInjection;
using Energinet.DataHub.Core.App.Common.Extensions.DependencyInjection;
using Energinet.DataHub.Core.App.WebApp.Extensions.Builder;
using Energinet.DataHub.Core.App.WebApp.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.ArchivedMessages.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.B2CWebApi.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.B2CWebApi.Security;
using Energinet.DataHub.EDI.DataAccess.UnitOfWork.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.IncomingMessages.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.MasterData.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.Outbox.Application.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

const string subsystemName = "EDI";

builder.Services
    // Swagger
    .AddSwaggerForWebApp(Assembly.GetExecutingAssembly(), "EDI B2C Web API")
    .AddApiVersioningForWebApp(new ApiVersion(1, 0))

    // Logging
    .AddApplicationInsightsForWebApp(subsystemName)

    // Health checks
    .AddHealthChecksForWebApp()

    // System timer
    .AddNodaTimeForApplication()

    // Durable client (orchestration)
    .AddDurableClient(builder.Configuration)

    // Modules
    .AddDataAccessUnitOfWorkModule()
    .AddOutboxModule(builder.Configuration)
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

    // Http
    .AddHttpClient()
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

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
