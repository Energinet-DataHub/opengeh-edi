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

using System.Text.Json.Serialization;
using BuildingBlocks.Application.Configuration.Logging;
using Energinet.DataHub.Core.App.WebApp.Authentication;
using Energinet.DataHub.Core.App.WebApp.Diagnostics.HealthChecks;
using Energinet.DataHub.Core.Logging.LoggingMiddleware;
using Energinet.DataHub.EDI.ArchivedMessages.Application.Configuration;
using Energinet.DataHub.EDI.B2CWebApi.Configuration;
using Energinet.DataHub.EDI.B2CWebApi.Configuration.Options;
using Energinet.DataHub.EDI.B2CWebApi.Security;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.Common.DateTime;
using Energinet.DataHub.EDI.Common.Serialization;
using Energinet.DataHub.EDI.IncomingMessages.Application.Configuration;
using Energinet.DataHub.EDI.Infrastructure.Configuration;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.OpenApi.Models;

namespace Energinet.DataHub.EDI.B2CWebApi;

public class Startup
{
    private const string DomainName = "EDI.B2CWebApi";
    private static readonly string[] _securityFields = { "Bearer" };

    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
        Configuration = configuration;
        Environment = environment;
    }

    public IConfiguration Configuration { get; }

    public IWebHostEnvironment Environment { get; }

    public void ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSwaggerGen(config =>
        {
            config.SwaggerDoc("v1", new OpenApiInfo { Title = "B2C web api for EDI", Version = "v1" });
            var securitySchema = new OpenApiSecurityScheme
            {
                Description =
                    "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer", },
            };

            config.AddSecurityDefinition("Bearer", securitySchema);
            config.SupportNonNullableReferenceTypes();
            config.UseAllOfToExtendReferenceSchemas();
            var securityRequirement = new OpenApiSecurityRequirement { { securitySchema, _securityFields }, };

            config.AddSecurityRequirement(securityRequirement);
        });
        serviceCollection.AddApplicationInsightsTelemetry(options => options.EnableAdaptiveSampling = false);
        serviceCollection.AddSingleton<ITelemetryInitializer, EnrichExceptionTelemetryInitializer>();
        serviceCollection.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        serviceCollection.AddHttpContextAccessor();

        serviceCollection.AddOptions<JwtOptions>().Bind(Configuration);
        serviceCollection.AddOptions<DateTimeOptions>().Bind(Configuration);

        serviceCollection.AddScoped<ISystemDateTimeProvider, SystemDateTimeProvider>();
        serviceCollection.AddHttpLoggingScope(DomainName);
        serviceCollection.AddSingleton<ISerializer, Serializer>();
        serviceCollection.AddScoped<AuthenticatedActor>();

        serviceCollection.AddArchivedMessagesModule(Configuration);
        serviceCollection.AddIncomingMessagesModule(Configuration);

        serviceCollection.AddJwtTokenSecurity(Configuration);
        serviceCollection.AddDateTimeConfiguration(Configuration);
        serviceCollection
            .AddHttpClient();

        serviceCollection.AddLiveHealthCheck();
        serviceCollection.AddExternalDomainServiceBusQueuesSenderHealthCheck(
            Configuration["SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_SEND"]!,
            Configuration["INCOMING_MESSAGES_QUEUE_NAME"]!);
        serviceCollection.AddSqlServerHealthCheck(Configuration["DB_CONNECTION_STRING"]!);

        var blobStorageUrl = Configuration["AZURE_STORAGE_ACCOUNT_URL"];
        serviceCollection.AddBlobStorageHealthCheck("Documents storage", blobStorageUrl != null ? new Uri(blobStorageUrl) : null!); // Send in null to not fail launching when running locally
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();

        if (Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseSwagger();

        app.UseSwaggerUI(options =>
        {
            if (!Environment.IsDevelopment()) return;
            options.EnableTryItOutByDefault();
        });

        app.UseLoggingScope();
        app.UseRouting();

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseUserMiddleware<FrontendUser>();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers().RequireAuthorization();
            endpoints.MapLiveHealthChecks();
            endpoints.MapReadyHealthChecks();
        });
    }
}
