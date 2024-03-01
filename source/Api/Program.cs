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
// limitations under the License.using System;

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using BuildingBlocks.Application.Configuration.Logging;
using BuildingBlocks.Application.Extensions.DependencyInjection;
using Energinet.DataHub.Core.App.FunctionApp.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.Api.Authentication;
using Energinet.DataHub.EDI.Api.Authentication.Certificate;
using Energinet.DataHub.EDI.Api.Configuration.Authentication;
using Energinet.DataHub.EDI.Api.Configuration.Middleware;
using Energinet.DataHub.EDI.Api.Configuration.Middleware.Authentication;
using Energinet.DataHub.EDI.Api.Configuration.Middleware.Correlation;
using Energinet.DataHub.EDI.ArchivedMessages.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.Common.DateTime;
using Energinet.DataHub.EDI.DataAccess.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.IncomingMessages.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.Infrastructure.Configuration.Authentication;
using Energinet.DataHub.EDI.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.IntegrationEvents.Application.Configuration;
using Energinet.DataHub.EDI.MasterData.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.MasterData.Interfaces;
using Energinet.DataHub.EDI.OutgoingMessages.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.Process.Application.Extensions.DependencyInjection;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Energinet.DataHub.EDI.Api
{
    public static class Program
    {
        public static async Task Main()
        {
            var runtime = RuntimeEnvironment.Default;
            var tokenValidationParameters = await GetTokenValidationParametersAsync(runtime).ConfigureAwait(false);
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            var host = ConfigureHost(tokenValidationParameters, runtime, config);

            await host.RunAsync().ConfigureAwait(false);
        }

        public static TokenValidationParameters DevelopmentTokenValidationParameters()
        {
#pragma warning disable CA5404 // Do not disable token validation checks
            return new TokenValidationParameters()
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateLifetime = false,
                SignatureValidator = (token, parameters) => new JwtSecurityToken(token),
            };
#pragma warning restore CA5404 // Do not disable token validation checks
        }

        public static IHost ConfigureHost(
            TokenValidationParameters tokenValidationParameters,
            RuntimeEnvironment runtime,
            IConfiguration configuration)
        {
            return new HostBuilder()
                .ConfigureFunctionsWorkerDefaults(
                    worker =>
                {
                    worker.UseMiddleware<UnHandledExceptionMiddleware>();
                    worker.UseMiddleware<CorrelationIdMiddleware>();
                    ConfigureAuthenticationMiddleware(worker);
                },
                    option =>
                {
                    option.EnableUserCodeException = true;
                })
                .ConfigureServices(services =>
                {
                    services.AddApplicationInsights()
                        .ConfigureFunctionsApplicationInsights()
                        .AddSingleton<ITelemetryInitializer, EnrichExceptionTelemetryInitializer>()
                        .AddB2BAuthentication(tokenValidationParameters);

                    CompositionRoot.Initialize(services)
                        .AddSystemClock(new SystemDateTimeProvider());

                    services.AddScoped(_ => new JwtTokenParser(tokenValidationParameters));
                    services.AddScoped<ICorrelationContext>(
                        _ =>
                        {
                            var correlationContext = new CorrelationContext();
                            if (!runtime.IsRunningLocally()) return correlationContext;
                            correlationContext.SetId(Guid.NewGuid().ToString());

                            return correlationContext;
                        });
                    services.AddLiveHealthCheck()
                        .AddExternalDomainServiceBusQueuesHealthCheck(
                            runtime.SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_MANAGE!,
                            runtime.EDI_INBOX_MESSAGE_QUEUE_NAME!,
                            runtime.WHOLESALE_INBOX_MESSAGE_QUEUE_NAME!)
                        .AddSqlServerHealthCheck(configuration);
                    services.AddBlobStorageHealthCheck("edi-web-jobs-storage", runtime.AzureWebJobsStorage!);
                    services.AddBlobStorageHealthCheck("edi-documents-storage", runtime.AZURE_STORAGE_ACCOUNT_URL!);

                    services.AddIntegrationEventModule()
                        .AddArchivedMessagesModule(configuration)
                        .AddIncomingMessagesModule(configuration)
                        .AddOutgoingMessagesModule(configuration)
                        .AddProcessModule(configuration)
                        .AddMasterDataModule(configuration)
                        .AddDataAccessModule(configuration);
                })
                .ConfigureLogging(logging =>
                {
                    logging.Services.Configure<LoggerFilterOptions>(options =>
                    {
                        var defaultRule = options.Rules.FirstOrDefault(rule =>
                            rule.ProviderName == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
                        if (defaultRule is not null)
                        {
                            options.Rules.Remove(defaultRule);
                        }
                    });
                })
                .Build();
        }

        public static IServiceCollection AddB2BAuthentication(this IServiceCollection services, TokenValidationParameters tokenValidationParameters)
        {
            services.AddScoped(sp => new JwtTokenParser(tokenValidationParameters))
                .AddTransient<IClientCertificateRetriever, HeaderClientCertificateRetriever>()
                .AddTransient<IAuthenticationMethod, BearerTokenAuthenticationMethod>()
                .AddTransient<IAuthenticationMethod, CertificateAuthenticationMethod>();

            services.AddScoped<IMarketActorAuthenticator>(sp =>
                new MarketActorAuthenticator(
                sp.GetRequiredService<IMasterDataClient>(),
                sp.GetRequiredService<AuthenticatedActor>(),
                sp.GetRequiredService<ILogger<MarketActorAuthenticator>>()));

            return services;
        }

        private static void ConfigureAuthenticationMiddleware(IFunctionsWorkerApplicationBuilder worker)
        {
            worker.UseMiddleware<MarketActorAuthenticatorMiddleware>();
        }

        private static async Task<TokenValidationParameters> GetTokenValidationParametersAsync(RuntimeEnvironment runtime)
        {
            var tenantId = Environment.GetEnvironmentVariable("B2C_TENANT_ID") ?? throw new InvalidOperationException("B2C tenant id not found.");
            var audience = Environment.GetEnvironmentVariable("BACKEND_SERVICE_APP_ID") ?? throw new InvalidOperationException("Backend service app id not found.");
            var metaDataAddress = $"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration";
            var openIdConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(metaDataAddress, new OpenIdConnectConfigurationRetriever());
            var stsConfig = await openIdConfigurationManager.GetConfigurationAsync().ConfigureAwait(false);
            return new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                RequireSignedTokens = true,
                ClockSkew = TimeSpan.Zero,
                ValidAudience = audience,
                IssuerSigningKeys = stsConfig.SigningKeys,
                ValidIssuer = stsConfig.Issuer,
            };
        }
    }
}
