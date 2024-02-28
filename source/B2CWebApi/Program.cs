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

using System.Text.Json.Serialization;
using BuildingBlocks.Application.Configuration.Logging;
using Energinet.DataHub.Core.App.FunctionApp.Extensions.DependencyInjection;
using Energinet.DataHub.Core.App.WebApp.Authentication;
using Energinet.DataHub.Core.App.WebApp.Diagnostics.HealthChecks;
using Energinet.DataHub.Core.Logging.LoggingMiddleware;
using Energinet.DataHub.EDI.ArchivedMessages.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.B2CWebApi.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.B2CWebApi.Security;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.Common.DateTime;
using Energinet.DataHub.EDI.Common.Serialization;
using Energinet.DataHub.EDI.IncomingMessages.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.Infrastructure.Extensions.DependencyInjection;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

const string domainName = "EDI.B2CWebApi";

const string securityProtocol = "Bearer";

builder.Logging
    .ClearProviders()
    .AddApplicationInsights();

builder.Services.AddSwaggerGen(
        config =>
        {
            config.SwaggerDoc("v1", new OpenApiInfo { Title = "B2C web api for EDI", Version = "v1" });
            var securitySchema = new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = securityProtocol, },
            };

            config.AddSecurityDefinition("Bearer", securitySchema);
            config.SupportNonNullableReferenceTypes();
            config.UseAllOfToExtendReferenceSchemas();
            var securityRequirement = new OpenApiSecurityRequirement { { securitySchema, new string[] { $"{securityProtocol}" } } };

            config.AddSecurityRequirement(securityRequirement);
        })
    .AddApplicationInsights()
    .AddSingleton<ITelemetryInitializer, EnrichExceptionTelemetryInitializer>()
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services
    .AddHttpContextAccessor()
    .AddScoped<ISystemDateTimeProvider, SystemDateTimeProvider>()
    .AddHttpLoggingScope(domainName)
    .AddSingleton<ISerializer, Serializer>()
    .AddScoped<AuthenticatedActor>()
    .AddIncomingMessagesModule(builder.Configuration)
    .AddArchivedMessagesModule(builder.Configuration)
    .AddJwtTokenSecurity(builder.Configuration)
    .AddDateTime(builder.Configuration)
    .AddHttpClient()
    .AddLiveHealthCheck();

var blobStorageUrl = builder.Configuration["AZURE_STORAGE_ACCOUNT_URL"];

builder.Services.AddBlobStorageHealthCheck(
        "edi-documents-storage",
        blobStorageUrl != null ? new Uri(blobStorageUrl) : null!);

// ***********************************************************************************************
// App building start here, aka Configure if one uses StartUp
// ***********************************************************************************************
var app = builder.Build();

var isDevelopment = app.Environment.IsDevelopment();

app.UseRouting();

if (isDevelopment)
    app.UseDeveloperExceptionPage();

app
    .UseSwagger()
    .UseSwaggerUI(
        options =>
        {
            if (!isDevelopment) return;
            options.EnableTryItOutByDefault();
        })
    .UseLoggingScope()
    .UseHttpsRedirection()
    .UseAuthentication()
    .UseAuthorization()
    .UseUserMiddleware<FrontendUser>();

app.MapControllers().RequireAuthorization();

app.MapLiveHealthChecks();

app.MapReadyHealthChecks();

app.Run();

// This is needed in order to test the dependency injection
public partial class Program
{
}
