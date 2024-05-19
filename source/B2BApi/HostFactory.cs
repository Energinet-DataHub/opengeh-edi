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
// limitations under the License.

using BuildingBlocks.Application.Extensions.DependencyInjection;
using Energinet.DataHub.Core.App.FunctionApp.Extensions.Builder;
using Energinet.DataHub.Core.App.FunctionApp.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.ArchivedMessages.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.B2BApi.Configuration.Middleware;
using Energinet.DataHub.EDI.B2BApi.Configuration.Middleware.Authentication;
using Energinet.DataHub.EDI.B2BApi.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.DataAccess.UnitOfWork.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.IncomingMessages.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.IntegrationEvents.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.MasterData.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.OutgoingMessages.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.Process.Application.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace Energinet.DataHub.EDI.B2BApi;

public static class HostFactory
{
    private const string DomainName = "EDI";

    public static IHost CreateHost(TokenValidationParameters tokenValidationParameters)
    {
        ArgumentNullException.ThrowIfNull(tokenValidationParameters);

        return new HostBuilder()
            .ConfigureFunctionsWorkerDefaults(
                worker =>
                {
                    // TODO: Add warning somewhere !!!
                    // => Any use of COnfigureAwait(false) within middleware which is used with Durable Function can cause it to freeze and never complete the Orchestration.
                    worker.UseMiddleware<UnHandledExceptionMiddleware>();
                    worker.UseMiddleware<MarketActorAuthenticatorMiddleware>();
                    worker.UseMiddleware<ExecutionContextMiddleware>();
                },
                option =>
                {
                    option.EnableUserCodeException = true;
                })
            .ConfigureServices(
                (context, services) =>
                {
                    services
                        // Logging
                        .AddApplicationInsightsForIsolatedWorker(DomainName)

                        // Health checks
                        .AddHealthChecksForIsolatedWorker()
                        .TryAddBlobStorageHealthCheck(
                            "edi-web-jobs-storage",
                            context.Configuration["AzureWebJobsStorage"]!)

                        // Data retention
                        .AddDataRetention()

                        // Security
                        .AddB2BAuthentication(tokenValidationParameters!)

                        // System timer
                        .AddSystemTimer()

                        // Serializer
                        .AddSerializer()

                        // Durable Task
                        .AddDurableClientFactory(options =>
                        {
                            options.ConnectionName = "AzureWebJobsStorage";
                            options.TaskHub = context.Configuration["OrchestrationsTaskHubName"]!;
                            options.IsExternalClient = true;
                        })

                        // Modules
                        .AddIntegrationEventModule(context.Configuration)
                        .AddArchivedMessagesModule(context.Configuration)
                        .AddIncomingMessagesModule(context.Configuration)
                        .AddOutgoingMessagesModule(context.Configuration)
                        .AddProcessModule(context.Configuration)
                        .AddMasterDataModule(context.Configuration)
                        .AddDataAccessUnitOfWorkModule(context.Configuration);
                })
            .ConfigureLogging(
                (hostingContext, logging) =>
                {
                    logging.AddLoggingConfigurationForIsolatedWorker(hostingContext);
                })
            .Build();
    }
}
