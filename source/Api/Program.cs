﻿// Copyright 2020 Energinet DataHub A/S
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

using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using BuildingBlocks.Application.Configuration.Logging;
using BuildingBlocks.Application.Extensions.DependencyInjection;
using Energinet.DataHub.Core.App.FunctionApp.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.Api.Configuration;
using Energinet.DataHub.EDI.Api.Configuration.Middleware;
using Energinet.DataHub.EDI.Api.Configuration.Middleware.Authentication;
using Energinet.DataHub.EDI.Api.Configuration.Middleware.Correlation;
using Energinet.DataHub.EDI.Api.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.ArchivedMessages.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.DataAccess.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.DataAccess.UnitOfWork.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.IncomingMessages.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.IntegrationEvents.Application.Configuration;
using Energinet.DataHub.EDI.MasterData.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.OutgoingMessages.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.Process.Application.Extensions.DependencyInjection;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Energinet.DataHub.EDI.Api
{
    public static class Program
    {
        public static async Task Main()
        {
            var runtime = RuntimeEnvironment.Default;
            var tokenValidationParameters =
                await TokenConfiguration.GetTokenValidationParametersAsync(runtime).ConfigureAwait(false);
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
                    worker.UseMiddleware<MarketActorAuthenticatorMiddleware>();
                },
                    option =>
                {
                    option.EnableUserCodeException = true;
                })
                .ConfigureServices(
                    (context, services) =>
                    {
                        services.AddApplicationInsights()
                            .ConfigureFunctionsApplicationInsights()
                            .AddSingleton<ITelemetryInitializer, EnrichExceptionTelemetryInitializer>()
                            .AddDataRetention()
                            .AddCorrelation(context.Configuration)
                            .AddLiveHealthCheck()
                            .AddExternalDomainServiceBusQueuesHealthCheck(
                                runtime.SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_MANAGE!,
                                runtime.EDI_INBOX_MESSAGE_QUEUE_NAME!,
                                runtime.WHOLESALE_INBOX_MESSAGE_QUEUE_NAME!)
                            .AddSqlServerHealthCheck(context.Configuration)
                            .AddSqlServerHealthCheck(configuration)
                            .AddB2BAuthentication(tokenValidationParameters)
                            .AddSystemClock()
                            .AddSerializer()
                            .AddLogging();
                        services.AddBlobStorageHealthCheck("edi-web-jobs-storage", runtime.AzureWebJobsStorage!);
                        services.AddBlobStorageHealthCheck("edi-documents-storage", runtime.AZURE_STORAGE_ACCOUNT_URL!);

                        services.AddIntegrationEventModule()
                        .AddArchivedMessagesModule(configuration)
                        .AddIncomingMessagesModule(configuration)
                        .AddOutgoingMessagesModule(configuration)
                        .AddProcessModule(configuration)
                        .AddMasterDataModule(configuration)
                        .AddDataAccessUnitOfWorkModule(configuration);
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
    }
}
