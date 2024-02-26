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
using Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents.IntegrationEventMappers;
using Energinet.DataHub.EDI.Process.Application.IntegrationEvents;
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
using Energinet.DataHub.EDI.Process.Infrastructure.InboxEvents;
using Energinet.DataHub.EDI.Process.Infrastructure.Processing;
using Energinet.DataHub.EDI.Process.Infrastructure.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Infrastructure.Wholesale;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.Process.Application.Extensions.DependencyInjection;

public static class ProcessExtensions
{
    public static IServiceCollection AddProcessModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ServiceBusClientOptions>()
            .Bind(configuration)
            .Validate(
                o => !string.IsNullOrEmpty(o.WHOLESALE_INBOX_MESSAGE_QUEUE_NAME),
                "WHOLESALE_INBOX_MESSAGE_QUEUE_NAME must be set");

        services.AddScopedSqlDbContext<ProcessContext>(configuration);

        //EventsConfiguration
        //TODO: can we move them out and delete ref to Infrastructure?
        services.AddTransient<IIntegrationEventProcessor, EnergyResultProducedV2Processor>()
            .AddTransient<IIntegrationEventProcessor, MonthlyAmountPerChargeResultProducedV1Processor>()
            .AddTransient<IIntegrationEventProcessor, AmountPerChargeResultProducedV1Processor>()
            .AddTransient<IInboxEventMapper, AggregatedTimeSeriesRequestAcceptedEventMapper>()
            .AddTransient<IInboxEventMapper, AggregatedTimeSeriesRequestRejectedMapper>();

        //ProcessingConfiguration
        services.AddScoped<DomainEventsAccessor>()
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehaviour<,>))
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(RaiseDomainEventsBehaviour<,>))
            .AddInternalCommands()
            .AddInboxEvents()
            .AddWholesaleInbox();

        //EnqueueMessageConfiguration
        services.AddTransient<INotificationHandler<EnqueueMessageEvent>, EnqueueMessageHandler>();

        //AggregationsConfiguration
        services.AddScoped<AggregationMessageResultFactory>();

        // RequestedAggregatedMeasureDataConfiguration
        services.AddTransient<IRequestHandler<SendAggregatedMeasureRequestToWholesale, Unit>, SendAggregatedMeasuredDataToWholesale>()
            .AddTransient<IRequestHandler<AcceptedAggregatedTimeSerie, Unit>, AcceptProcessWhenAcceptedAggregatedTimeSeriesIsAvailable>()
            .AddTransient<IRequestHandler<RejectedAggregatedTimeSeries, Unit>, RejectProcessWhenRejectedAggregatedTimeSeriesIsAvailable>()
            .AddTransient<INotificationHandler<AggregatedMeasureProcessIsInitialized>, NotifyWholesaleWhenAggregatedMeasureProcessIsInitialized>()
            .AddTransient<IRequestHandler<InitializeAggregatedMeasureDataProcessesCommand, Unit>, InitializeAggregatedMeasureDataProcessesHandler>()
            .AddTransient<INotificationHandler<AggregatedTimeSerieRequestWasAccepted>, WhenAnAcceptedAggregatedTimeSeriesRequestIsAvailable>()
            .AddTransient<INotificationHandler<AggregatedTimeSeriesRequestWasRejected>, WhenAnRejectedAggregatedTimeSeriesRequestIsAvailable>()
            .AddScoped<WholesaleInbox>()
            .AddScoped<IAggregatedMeasureDataProcessRepository, AggregatedMeasureDataProcessRepository>();

        return services;
    }
}
