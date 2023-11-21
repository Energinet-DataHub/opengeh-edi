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
using Energinet.DataHub.EDI.Common.DataRetention;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.AggregationResult;
using Energinet.DataHub.EDI.OutgoingMessages.Application.MarketDocuments.RejectRequestAggregatedMeasureData;
using Energinet.DataHub.EDI.OutgoingMessages.Application.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Contracts;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.OutgoingMessages.Queueing;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PeekResult = Energinet.DataHub.EDI.OutgoingMessages.Contracts.PeekResult;

namespace Energinet.DataHub.EDI.OutgoingMessages.Application.Configuration;

public static class ActorMessageQueueConfiguration
{
    public static void Configure(IServiceCollection services)
    {
        services.AddScopedSqlDbContext<ActorMessageQueueContext>();

        //AddMessageGenerationServices
        services.AddScoped<DocumentFactory>();
        services.AddScoped<IDocumentWriter, AggregationResultXmlDocumentWriter>();
        services.AddScoped<IDocumentWriter, AggregationResultJsonDocumentWriter>();
        services.AddScoped<IDocumentWriter, AggregationResultEbixDocumentWriter>();
        services.AddScoped<IDocumentWriter, RejectRequestAggregatedMeasureDataXmlDocumentWriter>();
        services.AddScoped<IDocumentWriter, RejectRequestAggregatedMeasureDataJsonDocumentWriter>();
        services.AddScoped<IDocumentWriter, RejectRequestAggregatedMeasureDataEbixDocumentWriter>();
        services.AddScoped<IMessageRecordParser, MessageRecordParser>();

        //MessageEnqueueingConfiguration
        services.AddTransient<IEnqueueMessage, EnqueueMessage>();
        services.AddScoped<IOutgoingMessageRepository, OutgoingMessageRepository>();

        //PeekConfiguration
        services.AddScoped<IActorMessageQueueRepository, ActorMessageQueueRepository>();
        services.AddScoped<IMarketDocumentRepository, MarketDocumentRepository>();
        services.AddTransient<IRequestHandler<PeekCommand, PeekResult>, PeekHandler>();

        //DequeConfiguration
        services.AddTransient<IRequestHandler<DequeueCommand, DequeCommandResult>, DequeueHandler>();
        services.AddTransient<IDataRetention, DequeuedBundlesRetention>();
    }
}
