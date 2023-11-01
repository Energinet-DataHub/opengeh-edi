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

using Energinet.DataHub.EDI.ActorMessageQueue.Application.OutgoingMessages;
using Energinet.DataHub.EDI.ActorMessageQueue.Application.OutgoingMessages.AggregationResult;
using Energinet.DataHub.EDI.ActorMessageQueue.Application.OutgoingMessages.Common;
using Energinet.DataHub.EDI.ActorMessageQueue.Application.OutgoingMessages.RejectRequestAggregatedMeasureData;
using Energinet.DataHub.EDI.ActorMessageQueue.Contracts;
using Energinet.DataHub.EDI.ActorMessageQueue.Domain.Documents;
using Energinet.DataHub.EDI.ActorMessageQueue.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.ActorMessageQueue.Domain.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.ActorMessageQueue.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.ActorMessageQueue.Infrastructure.OutgoingMessages;
using Energinet.DataHub.EDI.ActorMessageQueue.Infrastructure.OutgoingMessages.AggregationResult;
using Energinet.DataHub.EDI.ActorMessageQueue.Infrastructure.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.Infrastructure.Configuration;
using Energinet.DataHub.EDI.Infrastructure.Configuration.Queueing;
using Energinet.DataHub.EDI.Infrastructure.DataRetention;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PeekResult = Energinet.DataHub.EDI.ActorMessageQueue.Application.OutgoingMessages.PeekResult;

namespace Energinet.DataHub.EDI.ActorMessageQueue.Application.Configuration;

public static class ActorMessageQueueConfiguration
{
    public static void Configure(IServiceCollection services, string databaseConnectionString)
    {
        services.AddDbContext<DbContext, ActorMessageQueueContext>(options =>
            options.UseSqlServer(databaseConnectionString, y => y.UseNodaTime()));

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
        services.AddTransient<IRequestHandler<EnqueueMessageCommand>, EnqueueMessageHandler>();
        services.AddScoped<IOutgoingMessageRepository, OutgoingMessageRepository>();
        //services.AddScoped<OutgoingMessageEnqueuer>();

        //ProcessingConfiguration
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehaviour<,>));

        //PeekConfiguration
        services.AddScoped<EnqueueMessageHandler>();
        services.AddScoped<IActorMessageQueueRepository, ActorMessageQueueRepository>();
        services.AddScoped<IMarketDocumentRepository, MarketDocumentRepository>();
        services.AddTransient<IRequestHandler<PeekCommand, PeekResult>, PeekHandler>();

        //DequeConfiguration
        services.AddTransient<IRequestHandler<DequeueCommand, DequeCommandResult>, DequeueHandler>();
        services.AddTransient<IDataRetention, DequeuedBundlesRetention>();
    }
}
