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
using System.Threading.Tasks;
using B2B.Transactions.Infrastructure.Authentication.Bearer;
using B2B.Transactions.Infrastructure.Authentication.MarketActors;
using B2B.Transactions.Infrastructure.Configuration;
using B2B.Transactions.Infrastructure.Configuration.Correlation;
using Energinet.DataHub.Core.Logging.RequestResponseMiddleware;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace B2B.Transactions.Api
{
    public static class Program
    {
        public static async Task Main()
        {
            var tokenValidationParameters = await GetTokenValidationParametersAsync().ConfigureAwait(false);

            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults(worker =>
                {
                    worker.UseMiddleware<CorrelationIdMiddleware>();
                    worker.UseMiddleware<RequestResponseLoggingMiddleware>();
                    worker.UseMiddleware<BearerAuthenticationMiddleware>();
                    worker.UseMiddleware<ClaimsEnrichmentMiddleware>();
                    worker.UseMiddleware<MarketActorAuthenticatorMiddleware>();
                })
                .ConfigureServices(services =>
                {
                    var databaseConnectionString =
                        Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
                    CompositionRoot.Initialize(services)
                        .AddBearerAuthentication(tokenValidationParameters)
                        .AddDatabaseConnectionFactory(databaseConnectionString!)
                        .AddSystemClock(new SystemDateTimeProvider())
                        .AddDatabaseContext(databaseConnectionString!)
                        .AddCorrelationContext(sp =>
                        {
                            var correlationContext = new CorrelationContext();
                            if (!IsRunningLocally()) return correlationContext;
                            correlationContext.SetId(Guid.NewGuid().ToString());
                            correlationContext.SetParentId(Guid.NewGuid().ToString());

                            return correlationContext;
                        })
                        .AddTransactionQueue(
                            Environment.GetEnvironmentVariable("TRANSACTIONS_QUEUE_SENDER_CONNECTION_STRING")!,
                            Environment.GetEnvironmentVariable("TRANSACTIONS_QUEUE_NAME")!)
                        .AddRequestLogging(
                            Environment.GetEnvironmentVariable("REQUEST_RESPONSE_LOGGING_CONNECTION_STRING")!,
                            Environment.GetEnvironmentVariable("REQUEST_RESPONSE_LOGGING_CONTAINER_NAME")!);
                })
                .Build();

            await host.RunAsync().ConfigureAwait(false);
        }

        private static bool IsRunningLocally()
        {
            return Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") == "Development";
        }

        private static async Task<TokenValidationParameters> GetTokenValidationParametersAsync()
        {
            if (IsRunningLocally())
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
