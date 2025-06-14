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
// limitations under the License.

using DurableFunctionsMonitor.DotNetIsolated;
using Energinet.DataHub.Core.App.Common.Extensions.DependencyInjection;
using Energinet.DataHub.Core.App.FunctionApp.Extensions.Builder;
using Energinet.DataHub.Core.App.FunctionApp.Extensions.DependencyInjection;
using Energinet.DataHub.Core.Outbox.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.ArchivedMessages.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.AuditLog;
using Energinet.DataHub.EDI.B2BApi.Configuration.Middleware;
using Energinet.DataHub.EDI.B2BApi.Configuration.Middleware.Authentication;
using Energinet.DataHub.EDI.B2BApi.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.B2BApi.MeasurementsSynchronization;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.DataAccess.UnitOfWork.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.IntegrationEvents.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.MasterData.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.Outbox.Infrastructure;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using OutboxContext = Energinet.DataHub.EDI.Outbox.Infrastructure.OutboxContext;

namespace Energinet.DataHub.EDI.B2BApi;

public static class HostFactory
{
    private const string SubsystemName = "EDI";

    public static IHost CreateHost(TokenValidationParameters tokenValidationParameters)
    {
        ArgumentNullException.ThrowIfNull(tokenValidationParameters);

        return new HostBuilder()
            .ConfigureServices((context, services) =>
            {
                services
                    // Logging
                    .AddApplicationInsightsForIsolatedWorker(SubsystemName)

                    // Health checks
                    .AddHealthChecksForIsolatedWorker()

                    // Token credential
                    .AddTokenCredentialProvider()

                    // Azure App Configuration
                    .AddAzureAppConfiguration()

                    // Data retention
                    .AddDataRetention()

                    // Security
                    .AddB2BAuthentication(tokenValidationParameters)
                    .AddSubsystemAuthenticationForIsolatedWorker(context.Configuration)

                    // System timer
                    .AddNodaTimeForApplication()

                    // Encoder
                    .AddJavaScriptEncoder()

                    // Serializer
                    .AddSerializer()

                    // Modules
                    .AddIntegrationEventModule(context.Configuration)
                    .AddArchivedMessagesModule(context.Configuration)
                    .AddIncomingMessagesModule(context.Configuration)
                    .AddOutgoingMessagesModule(context.Configuration)
                    .AddMasterDataModule(context.Configuration)
                    .AddDataAccessUnitOfWorkModule()
                    .AddAuditLog()

                    // Audit log (outbox publisher)
                    .AddAuditLogOutboxPublisher(context.Configuration)

                    // Outbox context, client, processor and retention
                    .AddOutboxContext(context.Configuration)
                    .AddOutboxClient<OutboxContext>()
                    .AddOutboxProcessor<OutboxContext>()
                    .AddOutboxRetention()
                    .AddMeasurementsSynchronization()

                    // Enqueue messages from PM (using Edi Topic)
                    .AddEnqueueActorMessagesFromProcessManager()

                        // Configure Kestrel options
                        .Configure<KestrelServerOptions>(options =>
                        {
                            options.Limits.MaxRequestBodySize = 50 * 1024 * 1024; // 50mb
                        });
                })
            .ConfigureFunctionsWebApplication(builder =>
            {
                // Feature management
                //  * Enables middleware that handles refresh from Azure App Configuration
                builder.UseAzureAppConfigurationForIsolatedWorker();

                // If the endpoint is omitted from auth, we dont want to intercept exceptions.
                builder.UseWhen<UnHandledExceptionMiddleware>(
                    functionContext => functionContext.IsActorProtectedEndpoint());
                builder.UseWhen<MarketActorAuthenticatorMiddleware>(
                    functionContext => functionContext.IsActorProtectedEndpoint());
                builder.UseMiddleware<ExecutionContextMiddleware>();

                // Subsystem authentication
                builder.UseFunctionsAuthorization();

                // Host the Durable Function Monitor as a part of this app.
                // The Durable Function Monitor can be accessed at: {host url}/api/durable-functions-monitor
                builder.UseDurableFunctionsMonitor((settings, _) =>
                {
                    settings.Mode = DfmMode.ReadOnly;
                });
            })
            .ConfigureAppConfiguration((context, configBuilder) =>
            {
                // Feature management
                //  * Configure load/refresh from Azure App Configuration
                configBuilder.AddAzureAppConfigurationForIsolatedWorker();
            })
            .ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddLoggingConfigurationForIsolatedWorker(hostingContext.Configuration);
            })
            .Build();
    }
}
