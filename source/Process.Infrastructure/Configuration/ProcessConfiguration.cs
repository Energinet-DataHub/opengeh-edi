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

using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages;
using Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents.IntegrationEventMappers;
using Energinet.DataHub.EDI.Infrastructure.InboxEvents;
using Energinet.DataHub.EDI.Infrastructure.IncomingMessages.RequestAggregatedMeasureData;
using Energinet.DataHub.EDI.Infrastructure.Wholesale;
using Energinet.DataHub.EDI.Process.Application.OutgoingMessages;
using Energinet.DataHub.EDI.Process.Application.OutgoingMessages.Common;
using Energinet.DataHub.EDI.Process.Application.Transactions.Aggregations;
using Energinet.DataHub.EDI.Process.Domain.Documents;
using Energinet.DataHub.EDI.Process.Domain.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData.ProcessEvents;
using Energinet.DataHub.EDI.Process.Infrastructure.IntegrationEvents;
using Energinet.DataHub.EDI.Process.Infrastructure.OutgoingMessages;
using Energinet.DataHub.EDI.Process.Infrastructure.OutgoingMessages.AggregationResult;
using Energinet.DataHub.EDI.Process.Infrastructure.OutgoingMessages.Common;
using Energinet.DataHub.EDI.Process.Infrastructure.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.Process.Infrastructure.OutgoingMessages.RejectRequestAggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Infrastructure.Processing;
using Energinet.DataHub.EDI.Process.Infrastructure.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Infrastructure.Transactions.AggregatedMeasureData.Commands;
using Energinet.DataHub.EDI.Process.Infrastructure.Transactions.AggregatedMeasureData.Commands.Handlers;
using Energinet.DataHub.EDI.Process.Infrastructure.Transactions.AggregatedMeasureData.Notifications;
using Energinet.DataHub.EDI.Process.Infrastructure.Transactions.AggregatedMeasureData.Notifications.Handlers;
using Energinet.DataHub.EDI.Process.Infrastructure.Transactions.Aggregations;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PeekResult = Energinet.DataHub.EDI.Process.Application.OutgoingMessages.PeekResult;

namespace Energinet.DataHub.EDI.Process.Infrastructure.Configuration;

public static class ProcessConfiguration
{
    public static void Configure(IServiceCollection services)
    {
        services.AddTransient<IIntegrationEventMapper, CalculationResultCompletedMapper>();
        services.AddTransient<IInboxEventMapper, AggregatedTimeSeriesRequestAcceptedEventMapper>();
        services.AddTransient<IInboxEventMapper, AggregatedTimeSeriesRequestRejectedMapper>();

        //AddMessageGenerationServices
        services.AddScoped<DocumentFactory>();
        services.AddScoped<IDocumentWriter, AggregationResultXmlDocumentWriter>();
        services.AddScoped<IDocumentWriter, AggregationResultJsonDocumentWriter>();
        services.AddScoped<IDocumentWriter, AggregationResultEbixDocumentWriter>();
        services.AddScoped<IDocumentWriter, RejectRequestAggregatedMeasureDataXmlDocumentWriter>();
        services.AddScoped<IDocumentWriter, RejectRequestAggregatedMeasureDataJsonDocumentWriter>();
        services.AddScoped<IDocumentWriter, RejectRequestAggregatedMeasureDataEbixDocumentWriter>();
        services.AddScoped<IMessageRecordParser, MessageRecordParser>();

        services.AddScoped<IOutgoingMessageRepository, OutgoingMessageRepository>();
        services.AddScoped<OutgoingMessageEnqueuer>();

        services.AddScoped<DomainEventsAccessor>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RaiseDomainEventsBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(EnqueueOutgoingMessagesBehaviour<,>));

        //AggregationsConfiguration
        services.AddTransient<IRequestHandler<ForwardAggregationResult, Unit>, ForwardAggregationResultHandler>();
        //PeekConfiguration
        services.AddScoped<MessageEnqueuer>();
        services.AddScoped<IActorMessageQueueRepository, ActorMessageQueueRepository>();
        services.AddScoped<IMarketDocumentRepository, MarketDocumentRepository>();
        services.AddTransient<IRequestHandler<PeekCommand, PeekResult>, PeekHandler>();
        // RequestedAggregatedMeasureDataConfiguration
        services.AddTransient<IRequestHandler<SendAggregatedMeasureRequestToWholesale, Unit>, SendAggregatedMeasuredDataToWholesale>();
        services.AddTransient<IRequestHandler<AcceptedAggregatedTimeSerie, Unit>, AcceptProcessWhenAcceptedAggregatedTimeSeriesIsAvailable>();
        services.AddTransient<IRequestHandler<RejectedAggregatedTimeSeries, Unit>, RejectProcessWhenRejectedAggregatedTimeSeriesIsAvailable>();
        services.AddTransient<INotificationHandler<AggregatedMeasureProcessIsInitialized>, NotifyWholesaleWhenAggregatedMeasureProcessIsInitialized>();
        services.AddTransient<IRequestHandler<InitializeAggregatedMeasureDataProcessesCommand, Result>, InitializeAggregatedMeasureDataProcessesHandler>();
        services.AddTransient<INotificationHandler<AggregatedTimeSerieRequestWasAccepted>, WhenAnAcceptedAggregatedTimeSeriesRequestIsAvailable>();
        services.AddTransient<INotificationHandler<AggregatedTimeSeriesRequestWasRejected>, WhenAnRejectedAggregatedTimeSeriesRequestIsAvailable>();
        services.AddScoped<WholesaleInbox>();
        services.AddScoped<IAggregatedMeasureDataProcessRepository, AggregatedMeasureDataProcessRepository>();
    }
}
