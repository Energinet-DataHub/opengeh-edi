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
using System.Net.Http;
using System.Threading.Tasks;
using Messaging.Api.Configuration.Middleware.Authentication.Bearer;
using Messaging.Api.Configuration.Middleware.Authentication.MarketActors;
using Messaging.Api.Configuration.Middleware.Correlation;
using Messaging.Application.Actors;
using Messaging.Application.OutgoingMessages.Peek;
using Messaging.Application.Transactions.MoveIn;
using Messaging.CimMessageAdapter.Messages.Queues;
using Messaging.Infrastructure.Actors;
using Messaging.Infrastructure.Configuration;
using Messaging.Infrastructure.Configuration.Authentication;
using Messaging.Infrastructure.Configuration.MessageBus.RemoteBusinessServices;
using Messaging.Infrastructure.Transactions;
using Messaging.Infrastructure.Transactions.MoveIn;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Messaging.Api
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
                .ConfigureFunctionsWorkerDefaults(worker =>
                {
                    worker.UseMiddleware<CorrelationIdMiddleware>();
                    /*worker.UseMiddleware<RequestResponseLoggingMiddleware>();*/
                    ConfigureAuthenticationMiddleware(worker);
                })
                .ConfigureServices(services =>
                {
                    var databaseConnectionString = runtime.DB_CONNECTION_STRING;

                    services.AddSingleton(new MeteringPointServiceBusClientConfiguration(
                        "NotImplemented"));

                    services.AddSingleton(new EnergySupplyingServiceBusClientConfiguration(
                        "NotImplemented"));

                    services.AddSingleton(
                        _ => new RequestChangeOfSupplierTransaction(runtime.INCOMING_CHANGE_OF_SUPPLIER_MESSAGE_QUEUE_NAME!));

                    services.AddSingleton(
                        _ => new RequestChangeCustomerCharacteristicsTransaction("NotImplemented"));

                    CompositionRoot.Initialize(services)
                        .AddMessageBus(runtime.SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_SEND!)
                        .AddPeekConfiguration(new BundleConfiguration(runtime.MAX_NUMBER_OF_PAYLOADS_IN_BUNDLE))
                        .AddRemoteBusinessService<DummyRequest, DummyReply>("Dummy", "Dummy")
                        .AddBearerAuthentication(tokenValidationParameters)
                        .AddAuthentication(sp =>
                        {
                            if (runtime.IsRunningLocally() || runtime.PERFORMANCE_TEST_ENABLED)
                            {
                                Console.WriteLine("CompositionRoot: DevMarketActorAuthenticator");
                                return new DevMarketActorAuthenticator(
                                    sp.GetRequiredService<IActorLookup>(),
                                    sp.GetRequiredService<IActorRegistry>());
                            }

                            Console.WriteLine("CompositionRoot: MarketActorAuthenticator");
                            return new MarketActorAuthenticator(sp.GetRequiredService<IActorLookup>());
                        })
                        .AddDatabaseConnectionFactory(databaseConnectionString!)
                        .AddSystemClock(new SystemDateTimeProvider())
                        .AddDatabaseContext(databaseConnectionString!)
                        .AddCorrelationContext(sp =>
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
                        .AddHttpClientAdapter(sp => new HttpClientAdapter(sp.GetRequiredService<HttpClient>()))
                        .AddMoveInServices(
                            new MoveInSettings(
                                new MessageDelivery(
                                    new GridOperator()
                                    {
                                        GracePeriodInDaysAfterEffectiveDateIfNotUpdated = 15,
                                    }),
                                new BusinessService(new Uri("http://NotImplemented"))),
                            _ => new FakeMoveInRequester(),
                            _ => new FakeCustomerMasterDataClient(),
                            _ => new FakeMeteringPointMasterDataClient())
                        .AddMessageParserServices();

                    services.AddLiveHealthCheck();
                    services.AddExternalDomainServiceBusQueuesHealthCheck(
                        runtime.SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_MANAGE!,
                        runtime.INCOMING_CHANGE_OF_SUPPLIER_MESSAGE_QUEUE_NAME!);
                    services.AddSqlServerHealthCheck(runtime.DB_CONNECTION_STRING!);
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
            if (runtime.IsRunningLocally())
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
