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
using Azure.Messaging.ServiceBus;
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
using Messaging.Infrastructure.Configuration.MessageBus;
using Messaging.Infrastructure.Configuration.MessageBus.RemoteBusinessServices;
using Messaging.Infrastructure.Transactions;
using Messaging.Infrastructure.Transactions.MoveIn;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
                        runtime.MASTER_DATA_REQUEST_QUEUE_NAME!));

                    services.AddSingleton(new EnergySupplyingServiceBusClientConfiguration(
                        runtime.CUSTOMER_MASTER_DATA_REQUEST_QUEUE_NAME!));

                    services.AddSingleton<ServiceBusClient>(
                        _ => new ServiceBusClient(runtime.SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_SEND!));
                    services.AddSingleton<IServiceBusSenderFactory, ServiceBusSenderFactory>();

                    services.AddSingleton(
                        _ => new RequestChangeOfSupplierTransaction(runtime.INCOMING_CHANGE_OF_SUPPLIER_MESSAGE_QUEUE_NAME!));

                    services.AddSingleton(
                        _ => new RequestChangeCustomerCharacteristicsTransaction(runtime.INCOMING_CHANGE_CUSTOMER_CHARACTERISTICS_MESSAGE_QUEUE_NAME!));

                    CompositionRoot.Initialize(services)
                        .AddPeekConfiguration(new BundleConfiguration(runtime.MAX_NUMBER_OF_PAYLOADS_IN_BUNDLE))
                        .AddRemoteBusinessService<DummyRequest, DummyReply>("Dummy", "Dummy")
                        .AddBearerAuthentication(tokenValidationParameters)
                        .AddAuthentication(sp =>
                        {
                            if (runtime.IsRunningLocally())
                            {
                                return new DevMarketActorAuthenticator(
                                    sp.GetRequiredService<IActorLookup>(),
                                    sp.GetRequiredService<IActorRegistry>());
                            }

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
                                new BusinessService(new Uri(runtime.MOVE_IN_REQUEST_ENDPOINT!))))
                        .AddMessageParserServices();

                    services.AddLiveHealthCheck();
                    services.AddExternalDomainServiceBusQueuesHealthCheck(
                        runtime.SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_MANAGE!,
                        runtime.INCOMING_CHANGE_CUSTOMER_CHARACTERISTICS_MESSAGE_QUEUE_NAME!,
                        runtime.INCOMING_CHANGE_OF_SUPPLIER_MESSAGE_QUEUE_NAME!,
                        runtime.MESSAGE_REQUEST_QUEUE!,
                        runtime.CUSTOMER_MASTER_DATA_RESPONSE_QUEUE_NAME!,
                        runtime.CUSTOMER_MASTER_DATA_REQUEST_QUEUE_NAME!,
                        runtime.CUSTOMER_MASTER_DATA_UPDATE_RESPONSE_QUEUE_NAME!);
                    services.AddExternalServiceBusSubscriptionsHealthCheck(
                        runtime.SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_MANAGE!,
                        runtime.INTEGRATION_EVENT_TOPIC_NAME!,
                        runtime.CONSUMER_MOVED_IN_EVENT_SUBSCRIPTION_NAME!,
                        runtime.ENERGY_SUPPLIER_CHANGED_EVENT_SUBSCRIPTION_NAME!,
                        runtime.MARKET_PARTICIPANT_CHANGED_ACTOR_CREATED_SUBSCRIPTION_NAME!,
                        runtime.METERING_POINT_CREATED_EVENT_B2B_SUBSCRIPTION_NAME!);
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
