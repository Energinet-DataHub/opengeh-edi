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
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.Core.App.FunctionApp.Extensions.DependencyInjection;
using Energinet.DataHub.Core.Messaging.Communication;
using Energinet.DataHub.EDI.Api.Configuration.Middleware;
using Energinet.DataHub.EDI.Api.Configuration.Middleware.Authentication.Bearer;
using Energinet.DataHub.EDI.Api.Configuration.Middleware.Authentication.MarketActors;
using Energinet.DataHub.EDI.Api.Configuration.Middleware.Correlation;
using Energinet.DataHub.EDI.Application.Actors;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.MessageBus.RemoteBusinessServices;
using Energinet.DataHub.EDI.Infrastructure.Configuration;
using Energinet.DataHub.EDI.Infrastructure.Configuration.Authentication;
using Energinet.DataHub.EDI.Infrastructure.Wholesale;
using Energinet.DataHub.EDI.OutgoingMessages.Application.Configuration;
using Energinet.DataHub.EDI.Process.Application.Configuration;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.Contracts;
using Energinet.DataHub.Wholesale.Contracts.Events;
using Google.Protobuf.Reflection;
using Microsoft.Azure.Functions.Worker;
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
            var host = ConfigureHost(tokenValidationParameters, runtime);

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
            RuntimeEnvironment runtime)
        {
            return new HostBuilder()
                .ConfigureFunctionsWorkerDefaults(
                    worker =>
                {
                    worker.UseMiddleware<UnHandledExceptionMiddleware>();
                    worker.UseMiddleware<CorrelationIdMiddleware>();
                    /*worker.UseMiddleware<RequestResponseLoggingMiddleware>();*/
                    ConfigureAuthenticationMiddleware(worker);
                },
                    option =>
                {
                    option.EnableUserCodeException = true;
                })
                .ConfigureServices(services =>
                {
                    var databaseConnectionString = runtime.DB_CONNECTION_STRING;

                    services.AddSingleton(new WholesaleServiceBusClientConfiguration(
                        runtime.WHOLESALE_INBOX_MESSAGE_QUEUE_NAME!));

                    services.AddApplicationInsights();
                    services.ConfigureFunctionsApplicationInsights();

                    CompositionRoot.Initialize(services)
                        .AddMessageBus(runtime.SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_SEND!)
                        .AddRemoteBusinessService<DummyRequest, DummyReply>("Dummy", "Dummy")
                        .AddBearerAuthentication(tokenValidationParameters)
                        .AddAuthentication(sp =>
                        {
                            if (runtime.IsRunningLocally() || runtime.ALLOW_TEST_TOKENS)
                            {
                                return new DevMarketActorAuthenticator(
                                    sp.GetRequiredService<IActorRepository>(),
                                    sp.GetRequiredService<IActorRegistry>(),
                                    sp.GetRequiredService<IDatabaseConnectionFactory>(),
                                    sp.GetRequiredService<AuthenticatedActor>());
                            }

                            return new MarketActorAuthenticator(
                                sp.GetRequiredService<IActorRepository>(),
                                sp.GetRequiredService<AuthenticatedActor>());
                        })
                        .AddDatabaseConnectionFactory(databaseConnectionString!)
                        .AddSystemClock(new SystemDateTimeProvider())
                        .AddDatabaseContext(databaseConnectionString!)
                        .AddCorrelationContext(_ =>
                        {
                            var correlationContext = new CorrelationContext();
                            if (!runtime.IsRunningLocally()) return correlationContext;
                            correlationContext.SetId(Guid.NewGuid().ToString());

                            return correlationContext;
                        })
                        .AddRequestLogging(
                            runtime.REQUEST_RESPONSE_LOGGING_CONNECTION_STRING!,
                            runtime.REQUEST_RESPONSE_LOGGING_CONTAINER_NAME!)
                        .AddMessagePublishing()
                        .AddMessageParserServices();

                    services.AddLiveHealthCheck();
                    services.AddExternalDomainServiceBusQueuesHealthCheck(
                        runtime.SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_MANAGE!,
                        runtime.EDI_INBOX_MESSAGE_QUEUE_NAME!,
                        runtime.WHOLESALE_INBOX_MESSAGE_QUEUE_NAME!);
                    services.AddSqlServerHealthCheck(runtime.DB_CONNECTION_STRING!);
                    services.AddBlobStorageHealthCheck(runtime.AzureWebJobsStorage!);

                    var integrationEventDescriptors = new List<MessageDescriptor>
                    {
                        CalculationResultCompleted.Descriptor,
                        ActorActivated.Descriptor,
                        GridAreaOwnershipAssigned.Descriptor,
                    };
                    services.AddSubscriber<IntegrationEventHandler>(integrationEventDescriptors);

                    ActorMessageQueueConfiguration.Configure(services);
                    ProcessConfiguration.Configure(services);
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

        private static void ConfigureAuthenticationMiddleware(IFunctionsWorkerApplicationBuilder worker)
        {
            worker.UseMiddleware<BearerAuthenticationMiddleware>();
            worker.UseMiddleware<MarketActorAuthenticatorMiddleware>();
        }

        private static async Task<TokenValidationParameters> GetTokenValidationParametersAsync(RuntimeEnvironment runtime)
        {
            if (runtime.IsRunningLocally() || runtime.ALLOW_TEST_TOKENS)
            {
#pragma warning disable CA5404 // Do not disable token validation checks
                return DevelopmentTokenValidationParameters();
#pragma warning restore CA5404 // Do not disable token validation checks
            }

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
