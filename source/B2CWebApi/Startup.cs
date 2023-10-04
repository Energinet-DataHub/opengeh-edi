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
using Energinet.DataHub.Core.App.WebApp.Authentication;
using Energinet.DataHub.Core.App.WebApp.Authorization;
using Energinet.DataHub.Core.App.WebApp.Diagnostics.HealthChecks;
using Energinet.DataHub.Core.Logging.LoggingMiddleware;
using Energinet.DataHub.EDI.B2CWebApi.Clients;
using Energinet.DataHub.EDI.B2CWebApi.Configuration.Options;
using Energinet.DataHub.EDI.B2CWebApi.Security;
using Microsoft.OpenApi.Models;

namespace Energinet.DataHub.EDI.B2CWebApi;

public class Startup
{
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
            var securityRequirement = new OpenApiSecurityRequirement { { securitySchema, new[] { "Bearer" } }, };

            config.AddSecurityRequirement(securityRequirement);
        });
        serviceCollection.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        serviceCollection.AddHealthChecks();
        serviceCollection.AddHttpContextAccessor();

        serviceCollection.AddOptions<JwtOptions>().Bind(Configuration);
        serviceCollection.AddOptions<EdiOptions>().Bind(Configuration);

        AddJwtTokenSecurity(serviceCollection);
        serviceCollection
            .AddHttpClient();
        var ediClientOptions = Configuration.Get<EdiOptions>()!;
        serviceCollection.AddScoped(provider => new RequestAggregatedMeasureDataHttpClient(
            provider.GetRequiredService<IHttpClientFactory>(), new Uri(ediClientOptions.EDI_BASE_URL)));
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
            endpoints.MapControllers();
            endpoints.MapLiveHealthChecks();
            endpoints.MapReadyHealthChecks();
        });
    }

    /// <summary>
    /// Adds registrations of JwtTokenMiddleware and corresponding dependencies.
    /// </summary>
    private void AddJwtTokenSecurity(IServiceCollection serviceCollection)
    {
        var options = Configuration.Get<JwtOptions>()!;
        serviceCollection.AddJwtBearerAuthentication(options.EXTERNAL_OPEN_ID_URL, options.INTERNAL_OPEN_ID_URL, options.BACKEND_BFF_APP_ID);
        serviceCollection.AddUserAuthentication<FrontendUser, FrontendUserProvider>();
        serviceCollection.AddPermissionAuthorization();
    }
}
