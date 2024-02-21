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

using BuildingBlocks.Application.Configuration;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.AggregationResult;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.RejectRequestAggregatedMeasureData;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.WholesaleCalculations;
using Energinet.DataHub.EDI.OutgoingMessages.Application.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application.Configuration;

public static class OutgoingMessagesConfiguration
{
    public static void AddOutgoingMessagesModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddBuildingBlocks(configuration);

        services.AddScopedSqlDbContext<ActorMessageQueueContext>(configuration);

        //AddMessageGenerationServices
        services.AddScoped<DocumentFactory>();
        services.AddScoped<IDocumentWriter, AggregationResultXmlDocumentWriter>();
        services.AddScoped<IDocumentWriter, AggregationResultJsonDocumentWriter>();
        services.AddScoped<IDocumentWriter, AggregationResultEbixDocumentWriter>();
        services.AddScoped<IDocumentWriter, RejectRequestAggregatedMeasureDataXmlDocumentWriter>();
        services.AddScoped<IDocumentWriter, RejectRequestAggregatedMeasureDataJsonDocumentWriter>();
        services.AddScoped<IDocumentWriter, RejectRequestAggregatedMeasureDataEbixDocumentWriter>();
        services.AddScoped<IDocumentWriter, WholesaleCalculationXmlDocumentWriter>();
        services.AddScoped<IDocumentWriter, WholesaleCalculationResultEbixDocumentWriter>();
        services.AddScoped<IMessageRecordParser, MessageRecordParser>();

        //MessageEnqueueingConfiguration
        services.AddTransient<MessageEnqueuer>();
        services.AddScoped<IOutgoingMessageRepository, OutgoingMessageRepository>();

        //PeekConfiguration
        services.AddScoped<IActorMessageQueueRepository, ActorMessageQueueRepository>();
        services.AddScoped<IMarketDocumentRepository, MarketDocumentRepository>();
        services.AddTransient<MessagePeeker>();

        //DequeConfiguration
        services.AddTransient<MessageDequeuer>();
        services.AddTransient<IDataRetention, DequeuedBundlesRetention>();

        services.AddTransient<IOutgoingMessagesClient, OutgoingMessagesClient>();
    }
}
