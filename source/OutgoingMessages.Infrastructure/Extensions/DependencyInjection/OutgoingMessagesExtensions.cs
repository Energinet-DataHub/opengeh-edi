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
using Energinet.DataHub.Core.Databricks.SqlStatementExecution;
using Energinet.DataHub.Core.Databricks.SqlStatementExecution.Diagnostics.HealthChecks;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.DataAccess.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.OutgoingMessages.Application;
using Energinet.DataHub.EDI.OutgoingMessages.Application.UseCases;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.NotifyAggregatedMeasureData;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.NotifyWholesaleServices;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RejectRequestAggregatedMeasureData;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RejectRequestWholesaleSettlement;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.DocumentWriters.RSM012;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.CalculationResults.Statements;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.EnergyResults.Queries;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Databricks.WholesaleResults.Queries;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.Options;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Repositories.ActorMessageQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Repositories.Bundles;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Repositories.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Repositories.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Extensions.DependencyInjection;

public static class OutgoingMessagesExtensions
{
    public static IServiceCollection AddOutgoingMessagesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddBuildingBlocks(configuration)
            .AddScopedSqlDbContext<ActorMessageQueueContext>(configuration)
            .AddScoped<BuildingBlocks.Domain.ExecutionContext>();

        // Data access
        services.AddScoped<IActorMessageQueueContext, ActorMessageQueueContext>(sp =>
        {
            return sp.GetRequiredService<ActorMessageQueueContext>();
        });

        // AddMessageGenerationServices
        services.AddScoped<DocumentFactory>()
            .AddScoped<IDocumentWriter, NotifyAggregatedMeasureDataCimXmlDocumentWriter>()
            .AddScoped<IDocumentWriter, NotifyAggregatedMeasureDataCimJsonDocumentWriter>()
            .AddScoped<IDocumentWriter, NotifyAggregatedMeasureDataEbixDocumentWriter>()
            .AddScoped<IDocumentWriter, RejectRequestAggregatedMeasureDataCimXmlDocumentWriter>()
            .AddScoped<IDocumentWriter, RejectRequestAggregatedMeasureDataCimJsonDocumentWriter>()
            .AddScoped<IDocumentWriter, RejectRequestAggregatedMeasureDataEbixDocumentWriter>()
            .AddScoped<IDocumentWriter, NotifyWholesaleServicesCimXmlDocumentWriter>()
            .AddScoped<IDocumentWriter, NotifyWholesaleServicesCimJsonDocumentWriter>()
            .AddScoped<IDocumentWriter, NotifyWholesaleServicesEbixDocumentWriter>()
            .AddScoped<IDocumentWriter, RejectRequestWholesaleSettlementCimXmlDocumentWriter>()
            .AddScoped<IDocumentWriter, RejectRequestWholesaleSettlementCimJsonDocumentWriter>()
            .AddScoped<IDocumentWriter, RejectRequestWholesaleSettlementEbixDocumentWriter>()
            .AddScoped<IDocumentWriter, MeteredDateForMeasurementPointCimJsonDocumentWriter>()
            .AddScoped<IDocumentWriter, MeteredDateForMeasurementPointCimXmlDocumentWriter>()
            .AddScoped<IMessageRecordParser, MessageRecordParser>();

        // MessageEnqueueingConfiguration
        services.AddTransient<EnqueueMessage>()
            .AddTransient<DelegateMessage>()
            .AddTransient<IBundleRepository, BundleRepository>()
            .AddScoped<IOutgoingMessageRepository, OutgoingMessageRepository>()
            .AddTransient<IOutgoingMessagesClient, OutgoingMessagesClient>();

        // PeekConfiguration
        services.AddScoped<IActorMessageQueueRepository, ActorMessageQueueRepository>()
            .AddScoped<IMarketDocumentRepository, MarketDocumentRepository>()
            .AddTransient<PeekMessage>();

        // DequeConfiguration
        services.AddTransient<DequeueMessage>();

        // DataRetentionConfiguration
        services.AddTransient<IDataRetention, DequeuedBundlesRetention>();

        // CalculationResults
        services.AddOptions<DeltaTableOptions>()
            .Bind(configuration)
            .ValidateDataAnnotations();

        services.AddScoped<IWholesaleServicesQueries, WholesaleServicesQueries>();
        services.AddScoped<IAggregatedTimeSeriesQueries, AggregatedTimeSeriesQueries>();
        services.AddScoped<WholesaleServicesQuerySnippetProviderFactory>();
        services.AddScoped<AggregatedTimeSeriesQuerySnippetProviderFactory>();
        services
            .AddScoped<IWholesaleServicesDatabricksContract,
                AmountsPerChargeWholesaleServicesDatabricksContract>();
        services
            .AddScoped<IWholesaleServicesDatabricksContract,
                MonthlyAmountsPerChargeWholesaleServicesDatabricksContract>();
        services
            .AddScoped<IWholesaleServicesDatabricksContract,
                TotalMonthlyAmountWholesaleServicesDatabricksContract>();
        services
            .AddScoped<IAggregatedTimeSeriesDatabricksContract,
                EnergyPerBrpGaAggregatedTimeSeriesDatabricksContract>();
        services
            .AddScoped<IAggregatedTimeSeriesDatabricksContract,
                EnergyPerEsBrpGaAggregatedTimeSeriesDatabricksContract>();
        services
            .AddScoped<IAggregatedTimeSeriesDatabricksContract,
                EnergyPerGaAggregatedTimeSeriesDatabricksContract>();

        // Databricks
        services
            .AddOptions<EdiDatabricksOptions>()
            .BindConfiguration(EdiDatabricksOptions.SectionName)
            .ValidateDataAnnotations();
        services
            .AddNodaTimeForApplication()
            .AddScoped<EnergyResultEnumerator>()
            .AddScoped<WholesaleResultEnumerator>()
            .AddScoped<WholesaleResultActorsEnumerator>()
            .AddDatabricksSqlStatementExecution(configuration)
            .AddHealthChecks()
                .AddDatabricksSqlStatementApiHealthCheck(name: "DatabricksSqlStatementApi");

        return services;
    }
}
