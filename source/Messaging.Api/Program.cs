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
using Energinet.DataHub.MessageHub.Client.DataAvailable;
using Energinet.DataHub.MessageHub.Client.Storage;
using Messaging.Api.Configuration.Middleware.Authentication.Bearer;
using Messaging.Api.Configuration.Middleware.Authentication.MarketActors;
using Messaging.Api.Configuration.Middleware.Correlation;
using Messaging.Infrastructure.Configuration;
using Messaging.Infrastructure.Configuration.SystemTime;
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
                    CompositionRoot.Initialize(services)
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
                        .AddIncomingMessageQueue(
                            runtime.INCOMING_MESSAGE_QUEUE_SENDER_CONNECTION_STRING!,
                            runtime.INCOMING_MESSAGE_QUEUE_NAME!)
                        .AddRequestLogging(
                            runtime.REQUEST_RESPONSE_LOGGING_CONNECTION_STRING!,
                            runtime.REQUEST_RESPONSE_LOGGING_CONTAINER_NAME!)
                        .AddMessageStorage(sp =>
                        {
                            var messageRequestContext = sp.GetRequiredService<MessageRequestContext>();
                            var storageHandler = sp.GetRequiredService<IStorageHandler>();
                            return new MessageStorage(storageHandler, messageRequestContext);
                        })
                        .AddMessagePublishing(sp =>
                            new NewMessageAvailableNotifier(
                                sp.GetRequiredService<IDataAvailableNotificationSender>(),
                                sp.GetRequiredService<ActorLookup>()))
                        .AddMessageHubServices(
                            runtime.MESSAGEHUB_STORAGE_CONNECTION_STRING!,
                            runtime.MESSAGEHUB_STORAGE_CONTAINER_NAME!,
                            runtime.MESSAGEHUB_QUEUE_CONNECTION_STRING!,
                            runtime.MESSAGEHUB_DATA_AVAILABLE_QUEUE!,
                            runtime.MESSAGEHUB_DOMAIN_REPLY_QUEUE!)
                        .AddNotificationHandler<PublishNewMessagesOnTimeHasPassed, TimeHasPassed>()
                        .AddHttpClientAdapter(sp => new HttpClientAdapter(sp.GetRequiredService<HttpClient>()))
                        .AddServiceBusClient(
                            runtime.SHARED_SERVICE_BUS_SEND_CONNECTION_STRING!,
                            new RequestMasterDataConfiguration(
                                runtime.MASTER_DATA_REQUEST_QUEUE_NAME!,
                                "shared-service-bus-send-permission"))
                        .AddMoveInServices(new MoveInConfiguration(new Uri(runtime.MOVE_IN_REQUEST_ENDPOINT ?? throw new ArgumentException(nameof(runtime.MOVE_IN_REQUEST_ENDPOINT)))))
                        .AddMessageParserServices();

                    services.AddLiveHealthCheck();
                    services.AddInternalDomainServiceBusQueuesHealthCheck(
                        runtime.INCOMING_MESSAGE_QUEUE_MANAGE_CONNECTION_STRING!,
                        runtime.INCOMING_MESSAGE_QUEUE_NAME!,
                        runtime.MESSAGE_REQUEST_QUEUE!);
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
