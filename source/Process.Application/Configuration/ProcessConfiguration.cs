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

using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Configuration;
using Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents.IntegrationEventMappers;
using Energinet.DataHub.EDI.Infrastructure.InboxEvents;
using Energinet.DataHub.EDI.Infrastructure.Wholesale;
using Energinet.DataHub.EDI.Process.Application.IntegrationEvents;
using Energinet.DataHub.EDI.Process.Application.InternalCommands;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData.Commands;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData.Commands.Handlers;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData.Notifications;
using Energinet.DataHub.EDI.Process.Application.Transactions.AggregatedMeasureData.Notifications.Handlers;
using Energinet.DataHub.EDI.Process.Application.Transactions.Aggregations;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData.ProcessEvents;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Process.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.Process.Infrastructure.Processing;
using Energinet.DataHub.EDI.Process.Infrastructure.Transactions.AggregatedMeasureData;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.Process.Application.Configuration;

public static class ProcessConfiguration
{
    public static void AddProcessModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ServiceBusClientOptions>()
            .Bind(configuration)
            .Validate(
                o => !string.IsNullOrEmpty(o.INCOMING_MESSAGES_QUEUE_NAME),
                "INCOMING_MESSAGES_QUEUE_NAME must be set");

        services.AddScopedSqlDbContext<ProcessContext>();

        //EventsConfiguration
        //TODO: can we move them out and delete ref to Infrastructure?
        services.AddTransient<IIntegrationEventMapper, CalculationResultCompletedMapper>();
        services.AddTransient<IInboxEventMapper, AggregatedTimeSeriesRequestAcceptedEventMapper>();
        services.AddTransient<IInboxEventMapper, AggregatedTimeSeriesRequestRejectedMapper>();

        //ProcessingConfiguration
        services.AddScoped<DomainEventsAccessor>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RaiseDomainEventsBehaviour<,>));

        //EnqueueMessageConfiguration
        services.AddTransient<INotificationHandler<EnqueueMessageEvent>, EnqueueMessageHandler>();

        //AggregationsConfiguration
        services.AddTransient<IRequestHandler<ForwardAggregationResult, Unit>, ForwardAggregationResultHandler>();
        services.AddScoped<AggregationFactory>();

        // RequestedAggregatedMeasureDataConfiguration
        services.AddTransient<IRequestHandler<SendAggregatedMeasureRequestToWholesale, Unit>, SendAggregatedMeasuredDataToWholesale>();
        services.AddTransient<IRequestHandler<AcceptedAggregatedTimeSerie, Unit>, AcceptProcessWhenAcceptedAggregatedTimeSeriesIsAvailable>();
        services.AddTransient<IRequestHandler<RejectedAggregatedTimeSeries, Unit>, RejectProcessWhenRejectedAggregatedTimeSeriesIsAvailable>();
        services.AddTransient<INotificationHandler<AggregatedMeasureProcessIsInitialized>, NotifyWholesaleWhenAggregatedMeasureProcessIsInitialized>();
        services.AddTransient<IRequestHandler<InitializeAggregatedMeasureDataProcessesCommand, Unit>, InitializeAggregatedMeasureDataProcessesHandler>();
        services.AddTransient<INotificationHandler<AggregatedTimeSerieRequestWasAccepted>, WhenAnAcceptedAggregatedTimeSeriesRequestIsAvailable>();
        services.AddTransient<INotificationHandler<AggregatedTimeSeriesRequestWasRejected>, WhenAnRejectedAggregatedTimeSeriesRequestIsAvailable>();
        services.AddScoped<WholesaleInbox>();
        services.AddScoped<IAggregatedMeasureDataProcessRepository, AggregatedMeasureDataProcessRepository>();

        InternalCommandConfiguration.Configure(services);
    }
}
