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
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Energinet.DataHub.MessageHub.Client.DataAvailable;
using Energinet.DataHub.MessageHub.Client.Storage;
using Messaging.Api.Configuration.Middleware.Authentication.Bearer;
using Messaging.Api.Configuration.Middleware.Authentication.MarketActors;
using Messaging.Api.Configuration.Middleware.Correlation;
using Messaging.Application.Configuration;
using Messaging.Application.Configuration.TimeEvents;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.Transactions.MoveIn;
using Messaging.CimMessageAdapter.Messages.Queues;
using Messaging.Infrastructure.Configuration;
using Messaging.Infrastructure.Configuration.MessageBus;
using Messaging.Infrastructure.OutgoingMessages;
using Messaging.Infrastructure.OutgoingMessages.Requesting;
using Messaging.Infrastructure.Transactions;
using Messaging.Infrastructure.Transactions.MoveIn;
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

        public static IHost ConfigureHost(TokenValidationParameters tokenValidationParameters, RuntimeEnvironment runtime)
        {
            return new HostBuilder()
                .ConfigureFunctionsWorkerDefaults(worker =>
                {
                    worker.UseMiddleware<CorrelationIdMiddleware>();
                    /*worker.UseMiddleware<RequestResponseLoggingMiddleware>();*/
                    worker.UseMiddleware<BearerAuthenticationMiddleware>();
                    worker.UseMiddleware<ClaimsEnrichmentMiddleware>();
                    worker.UseMiddleware<MarketActorAuthenticatorMiddleware>();
                })
                .ConfigureServices(services =>
                {
                    var databaseConnectionString = runtime.DB_CONNECTION_STRING;

                    var meteringPointServiceBusClientConfiguration =
                        new MeteringPointServiceBusClientConfiguration(
                            runtime.MASTER_DATA_REQUEST_QUEUE_NAME!,
                            "MeteringPointsSenderClient");
                    services.AddSingleton(meteringPointServiceBusClientConfiguration);
                    services.AddAzureServiceBusClient(new ServiceBusClientConfiguration(runtime.SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_SEND, meteringPointServiceBusClientConfiguration));

                    var energySupplyingServiceBusClientConfiguration =
                        new EnergySupplyingServiceBusClientConfiguration(
                            runtime.CUSTOMER_MASTER_DATA_REQUEST_QUEUE_NAME!,
                            "EnergySupplyingSenderClient");
                    services.AddSingleton(energySupplyingServiceBusClientConfiguration);
                    services.AddAzureServiceBusClient(new ServiceBusClientConfiguration(runtime.SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_SEND, energySupplyingServiceBusClientConfiguration));

                    services.AddSingleton<ServiceBusClient>(
                        _ => new ServiceBusClient(runtime.SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_SEND!));

                    services.AddSingleton(
                        _ => new RequestChangeOfSupplierTransaction(runtime.INCOMING_CHANGE_OF_SUPPLIER_MESSAGE_QUEUE_NAME!));

                    services.AddSingleton(
                        _ => new RequestChangeCustomerCharacteristicsTransaction(runtime.INCOMING_CHANGE_CUSTOMER_CHARACTERISTICS_MESSAGE_QUEUE_NAME!));

                    services.AddSingleton<IServiceBusSenderAdapter>(sp => new ServiceBusSenderAdapter(sp.GetRequiredService<ServiceBusClient>(), "Dummy"));

                    CompositionRoot.Initialize(services)
                        .AddRemoteBusinessService<DummyRequest, DummyReply>(sp =>
                        {
                            var adapter = sp.GetRequiredService<IServiceBusSenderAdapter>();
                            return new RemoteBusinessService<DummyRequest, DummyReply>(adapter, "Dummy");
                        })
                        .AddBearerAuthentication(tokenValidationParameters)
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
                        .AddMessageStorage(sp =>
                        {
                            var storageHandler = sp.GetRequiredService<IStorageHandler>();
                            return new MessageStorage(storageHandler);
                        })
                        .AddMessagePublishing(sp =>
                            new NewMessageAvailableNotifier(
                                sp.GetRequiredService<IDataAvailableNotificationSender>(),
                                sp.GetRequiredService<IActorLookup>(),
                                sp.GetRequiredService<ICorrelationContext>()))
                        .AddMessageHubServices(
                            runtime.MESSAGEHUB_STORAGE_CONNECTION_STRING!,
                            runtime.MESSAGEHUB_STORAGE_CONTAINER_NAME!,
                            runtime.MESSAGEHUB_QUEUE_CONNECTION_STRING!,
                            runtime.MESSAGEHUB_DATA_AVAILABLE_QUEUE!,
                            runtime.MESSAGEHUB_DOMAIN_REPLY_QUEUE!)
                        .AddNotificationHandler<PublishNewMessagesOnTimeHasPassed, TenSecondsHasHasPassed>()
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
