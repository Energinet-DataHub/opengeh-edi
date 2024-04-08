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
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.DataAccess.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.NotifyAggregatedMeasureData;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.NotifyWholesaleServices;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.RejectRequestAggregatedMeasureData;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.RejectRequestWholesaleSettlement;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application.Extensions.DependencyInjection;

public static class OutgoingMessagesExtensions
{
    public static IServiceCollection AddOutgoingMessagesModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddBuildingBlocks(configuration)
            .AddScopedSqlDbContext<ActorMessageQueueContext>(configuration);

        //AddMessageGenerationServices
        services.AddScoped<DocumentFactory>()
            .AddScoped<IDocumentWriter, NotifyAggregatedMeasureDataXmlDocumentWriter>()
            .AddScoped<IDocumentWriter, NotifyAggregatedMeasureDataJsonDocumentWriter>()
            .AddScoped<IDocumentWriter, NotifyAggregatedMeasureDataEbixDocumentWriter>()
            .AddScoped<IDocumentWriter, RejectRequestAggregatedMeasureDataXmlDocumentWriter>()
            .AddScoped<IDocumentWriter, RejectRequestAggregatedMeasureDataJsonDocumentWriter>()
            .AddScoped<IDocumentWriter, RejectRequestAggregatedMeasureDataEbixDocumentWriter>()
            .AddScoped<IDocumentWriter, NotifyWholesaleServicesXmlDocumentWriter>()
            .AddScoped<IDocumentWriter, NotifyWholesaleServicesJsonDocumentWriter>()
            .AddScoped<IDocumentWriter, NotifyWholesaleServicesEbixDocumentWriter>()
            .AddScoped<IDocumentWriter, RejectRequestWholesaleSettlementXmlDocumentWriter>()
            .AddScoped<IDocumentWriter, RejectRequestWholesaleSettlementJsonDocumentWriter>()
            .AddScoped<IDocumentWriter, RejectRequestWholesaleSettlementEbixDocumentWriter>()
            .AddScoped<IMessageRecordParser, MessageRecordParser>();

        //MessageEnqueueingConfiguration
        services.AddTransient<MessageEnqueuer>()
            .AddTransient<OutgoingMessageDelegator>()
            .AddScoped<IOutgoingMessageRepository, OutgoingMessageRepository>()
            .AddTransient<IOutgoingMessagesClient, OutgoingMessagesClient>();

        //PeekConfiguration
        services.AddScoped<IActorMessageQueueRepository, ActorMessageQueueRepository>()
            .AddScoped<IMarketDocumentRepository, MarketDocumentRepository>()
            .AddTransient<MessagePeeker>();

        //DequeConfiguration
        services.AddTransient<MessageDequeuer>();

        //DataRetentionConfiguration
        services.AddTransient<IDataRetention, DequeuedBundlesRetention>();

        return services;
    }
}
