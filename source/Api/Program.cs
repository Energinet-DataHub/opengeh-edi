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
using Api.Configuration.Middleware;
using Api.Configuration.Middleware.Authentication.Bearer;
using Api.Configuration.Middleware.Authentication.MarketActors;
using Api.Configuration.Middleware.Correlation;
using Application.Actors;
using Application.Configuration.DataAccess;
using Application.Transactions.MoveIn;
using CimMessageAdapter.Messages.Queues;
using Infrastructure.Configuration;
using Infrastructure.Configuration.Authentication;
using Infrastructure.Configuration.MessageBus.RemoteBusinessServices;
using Infrastructure.Transactions;
using Infrastructure.Transactions.MoveIn;
using Infrastructure.WholeSale;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Api
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
                    worker.UseMiddleware<UnHandledExceptionMiddleware>();
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

                    services.AddSingleton(new WholeSaleServiceBusClientConfiguration(
                        runtime.EDI_INBOX_MESSAGE_QUEUE_NAME!));

                    services.AddSingleton(
                        _ => new RequestChangeOfSupplierTransaction(runtime.INCOMING_CHANGE_OF_SUPPLIER_MESSAGE_QUEUE_NAME!));

                    services.AddSingleton(
                        _ => new RequestChangeCustomerCharacteristicsTransaction("NotImplemented"));

                    services.AddSingleton(
                        _ => new RequestAggregatedMeasureDataTransactionQueues(runtime.INCOMING_AGGREGATED_MEASURE_DATA_QUEUE_NAME!));

                    CompositionRoot.Initialize(services)
                        .AddMessageBus(runtime.SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_SEND!)
                        .AddPeekConfiguration()
                        .AddAggregationsConfiguration()
                        .AddRemoteBusinessService<DummyRequest, DummyReply>("Dummy", "Dummy")
                        .AddBearerAuthentication(tokenValidationParameters)
                        .AddAuthentication(sp =>
                        {
                            if (runtime.IsRunningLocally() || runtime.ALLOW_TEST_TOKENS)
                            {
                                return new DevMarketActorAuthenticator(
                                    sp.GetRequiredService<IActorLookup>(),
                                    sp.GetRequiredService<IActorRegistry>(),
                                    sp.GetRequiredService<IDatabaseConnectionFactory>());
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
                        .AddAggregatedMeasureDataServices()
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
                        runtime.INCOMING_CHANGE_OF_SUPPLIER_MESSAGE_QUEUE_NAME!,
                        runtime.INCOMING_AGGREGATED_MEASURE_DATA_QUEUE_NAME!,
                        runtime.EDI_INBOX_MESSAGE_QUEUE_NAME!);
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
