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

using Energinet.DataHub.Core.Messaging.Communication.Extensions.Options;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.DataAccess.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.Process.Application.ProcessInitializationHandlers;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData.Commands;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData.Commands.Handlers;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData.Notifications;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData.Notifications.Handlers;
using Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices;
using Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices.Commands;
using Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices.Commands.Handlers;
using Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices.Notifications;
using Energinet.DataHub.EDI.Process.Application.Transactions.WholesaleServices.Notifications.Handlers;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData.ProcessEvents;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices.ProcessEvents;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.Process.Infrastructure.Processing;
using Energinet.DataHub.EDI.Process.Infrastructure.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Infrastructure.Transactions.WholesaleServices;
using Energinet.DataHub.EDI.Process.Infrastructure.Wholesale;
using Energinet.DataHub.EDI.Process.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.Process.Application.Extensions.DependencyInjection;

public static class ProcessExtensions
{
    public static IServiceCollection AddProcessModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // Options
        services.AddOptions<ServiceBusNamespaceOptions>()
            .BindConfiguration(ServiceBusNamespaceOptions.SectionName)
            .ValidateDataAnnotations();

        services
            .AddOptionsWithValidateOnStart<EdiInboxQueueOptions>()
            .BindConfiguration(EdiInboxQueueOptions.SectionName)
            .ValidateDataAnnotations();

        services
            .AddScopedSqlDbContext<ProcessContext>(configuration)
            .AddMediatR()
            .AddScoped<BuildingBlocks.Domain.ExecutionContext>();

        // ProcessingConfiguration
        services.AddScoped<DomainEventsAccessor>()
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehaviour<,>))
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(RaiseDomainEventsBehaviour<,>))
            .AddInternalCommands()
            .AddInboxEvents()
            .AddWholesaleInbox(configuration);

        // EnqueueMessageConfiguration
        services.AddTransient<INotificationHandler<EnqueuedAcceptedWholesaleServicesEvent>, EnqueuedWholesaleServicesMessageHandler>();
        services.AddTransient<INotificationHandler<EnqueueAcceptedEnergyResultMessageEvent>, EnqueueAcceptedEnergyResultMessageHandler>();
        services.AddTransient<INotificationHandler<EnqueueRejectedEnergyResultMessageEvent>, EnqueueRejectedEnergyResultMessageHandler>();
        services.AddTransient<INotificationHandler<EnqueueRejectedWholesaleServicesMessageEvent>,
                EnqueueRejectedWholesaleServicesMessageHandler>();

        // ProcessInitialization handlers Configuration
        services.AddTransient<IProcessInitializationHandler, InitializeAggregatedMeasureDataHandler>();
        services.AddTransient<IProcessInitializationHandler, InitializeWholesaleServicesProcessHandler>();
        services.AddTransient<IProcessInitializationHandler, InitializeMeteredDataForMeteringPointHandler>();

        // ProcessInitializationClient Configuration
        services.AddTransient<IProcessClient, ProcessClient>();

        // RequestedAggregatedMeasureDataConfiguration
        services
            .AddTransient<IRequestHandler<SendAggregatedMeasureDataRequestToWholesale, Unit>,
                SendAggregatedMeasureDataRequestToWholesaleHandler>()
            .AddTransient<IRequestHandler<AcceptedEnergyResultTimeSeriesCommand, Unit>, AcceptProcessWhenAcceptedEnergyResultTimeSeriesIsAvailable>()
            .AddTransient<IRequestHandler<RejectedAggregatedTimeSeries, Unit>, RejectProcessWhenRejectedAggregatedTimeSeriesIsAvailable>()
            .AddTransient<INotificationHandler<AggregatedMeasureProcessIsInitialized>, NotifyWholesaleWhenAggregatedMeasureProcessIsInitialized>()
            .AddTransient<INotificationHandler<NotifyWholesaleThatAggregatedMeasureDataIsRequested>,
                NotifyWholesaleThatAggregatedMeasureDataIsRequestedHandler>()
            .AddTransient<IRequestHandler<InitializeAggregatedMeasureDataProcessesCommand, Unit>, InitializeAggregatedMeasureDataProcessesHandler>()
            .AddTransient<INotificationHandler<AggregatedTimeSeriesRequestWasAccepted>, WhenAnAcceptedAggregatedTimeSeriesRequestIsAvailable>()
            .AddTransient<INotificationHandler<AggregatedTimeSeriesRequestWasRejected>, WhenAnRejectedAggregatedTimeSeriesRequestIsAvailable>()
            .AddScoped<WholesaleInboxClient>()
            .AddScoped<IAggregatedMeasureDataProcessRepository, AggregatedMeasureDataProcessRepository>();

        // RequestedWholesaleServicesConfiguration
        services
            .AddTransient<IRequestHandler<AcceptedWholesaleServices, Unit>, AcceptWholesaleServicesWhenAnAcceptedWholesaleServicesRequestIsAvailable>()
            .AddTransient<IRequestHandler<SendWholesaleServicesRequestToWholesale, Unit>, SendWholesaleServicesRequestToWholesaleHandler>()
            .AddTransient<INotificationHandler<WholesaleServicesRequestWasAccepted>, WhenAnAcceptedWholesaleServicesRequestIsAvailable>()
            .AddTransient<INotificationHandler<WholesaleServicesProcessIsInitialized>, NotifyWholesaleWhenWholesaleServicesProcessIsInitialized>()
            .AddTransient<INotificationHandler<NotifyWholesaleThatWholesaleServicesIsRequested>, NotifyWholesaleThatWholesaleServicesIsRequestedHandler>()
            .AddTransient<IRequestHandler<InitializeWholesaleServicesProcessesCommand, Unit>, InitializeWholesaleServicesProcessesHandler>()
            .AddScoped<IWholesaleServicesProcessRepository, WholesaleServicesProcessRepository>()
            .AddTransient<IRequestHandler<RejectedWholesaleServices, Unit>,
                RejectProcessWhenRejectedWholesaleServicesIsAvailable>()
            .AddTransient<INotificationHandler<WholesaleServicesRequestWasRejected>,
                WhenARejectedWholesaleServicesRequestIsAvailable>()

            // Health checks
            .TryAddExternalDomainServiceBusQueuesHealthCheck(
                configuration.GetSection(ServiceBusNamespaceOptions.SectionName).Get<ServiceBusNamespaceOptions>()!.FullyQualifiedNamespace,
                configuration.GetSection(EdiInboxQueueOptions.SectionName).Get<EdiInboxQueueOptions>()!.QueueName);
        return services;
    }
}
