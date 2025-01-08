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

using Energinet.DataHub.Core.App.Common.Extensions.DependencyInjection;
using Energinet.DataHub.Core.App.FunctionApp.Extensions.Builder;
using Energinet.DataHub.Core.App.FunctionApp.Extensions.DependencyInjection;
using Energinet.DataHub.Core.Outbox.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.ArchivedMessages.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.AuditLog;
using Energinet.DataHub.EDI.B2BApi.Configuration.Middleware;
using Energinet.DataHub.EDI.B2BApi.Configuration.Middleware.Authentication;
using Energinet.DataHub.EDI.B2BApi.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.DataAccess.UnitOfWork.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.IncomingMessages.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.IntegrationEvents.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.MasterData.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.Outbox.Infrastructure;
using Energinet.DataHub.EDI.OutgoingMessages.Application.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.Process.Application.Extensions.DependencyInjection;
using Energinet.DataHub.Wholesale.CalculationResults.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.Wholesale.Edi.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker;
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
            .ConfigureFunctionsWebApplication(
                builder =>
                {
                    // If the endpoint is omitted from auth, we dont want to intercept exceptions.
                    builder.UseWhen<UnHandledExceptionMiddleware>(
                        functionContext => functionContext.IsHttpTriggerAndNotHealthCheck());
                    builder.UseMiddleware<SuppressOperationCanceledExceptionMiddleware>();
                    builder.UseWhen<MarketActorAuthenticatorMiddleware>(
                        functionContext => functionContext.IsHttpTriggerAndNotHealthCheck());
                    builder.UseMiddleware<ExecutionContextMiddleware>();
                })
            .ConfigureServices(
                (context, services) =>
                {
                    services
                        // Logging
                        .AddApplicationInsightsForIsolatedWorker(SubsystemName)

                        // Health checks
                        .AddHealthChecksForIsolatedWorker()

                        // Data retention
                        .AddDataRetention()

                        // Security
                        .AddB2BAuthentication(tokenValidationParameters)

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
                        .AddProcessModule(context.Configuration)
                        .AddMasterDataModule(context.Configuration)
                        .AddDataAccessUnitOfWorkModule()
                        .AddAuditLog()
                        .AddEdiModule(context.Configuration)
                        .AddCalculationResultsModule(context.Configuration)

                        // Audit log (outbox publisher)
                        .AddAuditLogOutboxPublisher(context.Configuration)

                        // Outbox context, client, processor and retention
                        .AddOutboxContext(context.Configuration)
                        .AddOutboxClient<OutboxContext>()
                        .AddOutboxProcessor<OutboxContext>()
                        .AddOutboxRetention()

                        // EDI Topic
                        .AddEdiTopic();
                })
            .ConfigureLogging(
                (hostingContext, logging) =>
                {
                    logging.AddLoggingConfigurationForIsolatedWorker(hostingContext);
                })
            .Build();
    }
}
