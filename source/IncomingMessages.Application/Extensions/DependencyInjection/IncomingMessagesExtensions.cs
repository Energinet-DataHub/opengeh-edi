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

using Azure.Identity;
using Azure.Messaging.ServiceBus;
using BuildingBlocks.Application.Extensions.DependencyInjection;
using Energinet.DataHub.Core.App.Common.Diagnostics.HealthChecks;
using Energinet.DataHub.Core.Messaging.Communication.Extensions.Builder;
using Energinet.DataHub.Core.Messaging.Communication.Extensions.DependencyInjection;
using Energinet.DataHub.Core.Messaging.Communication.Extensions.Options;
using Energinet.DataHub.EDI.DataAccess.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.IncomingMessages.Application.UseCases;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers.AggregatedMeasureDataRequestMessageParsers;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParsers.WholesaleSettlementMessageParsers;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Repositories.MessageId;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Repositories.TransactionId;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Response;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Schemas.Cim.Json;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Schemas.Cim.Xml;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Energinet.DataHub.EDI.IncomingMessages.Application.Extensions.DependencyInjection;

public static class IncomingMessagesExtensions
{
    public static IServiceCollection AddIncomingMessagesModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddFeatureFlags()
            .AddServiceBusClientForApplication(configuration)
            .AddDapperConnectionToDatabase(configuration)
            .AddScopedSqlDbContext<IncomingMessagesContext>(configuration)
            .AddScoped<IIncomingMessageClient, IncomingMessageClient>()
            .AddScoped<ReceiveIncomingMarketMessage>()
            .AddScoped<DelegateIncomingMessage>()
            .AddScoped<ITransactionIdRepository, TransactionIdRepository>()
            .AddScoped<IMessageIdRepository, MessageIdRepository>()
            .AddScoped<IMarketMessageParser, AggregatedMeasureDataXmlMessageParser>()
            .AddScoped<IMarketMessageParser, AggregatedMeasureDataJsonMessageParser>()
            .AddScoped<IMarketMessageParser, AggregatedMeasureDataB2CJsonMessageParser>()
            .AddScoped<IMarketMessageParser, WholesaleSettlementJsonMessageParser>()
            .AddScoped<IMarketMessageParser, WholesaleSettlementXmlMessageParser>()
            .AddScoped<IMarketMessageParser, WholesaleSettlementB2CJsonMessageParser>()
            .AddScoped<MarketMessageParser>()
            .AddScoped<ISenderAuthorizer, SenderAuthorizer>()
            .AddScoped<ValidateIncomingMessage>()
            .AddSingleton<IProcessTypeValidator, ProcessTypeValidator>()
            .AddSingleton<IMessageTypeValidator, MessageTypeValidator>()
            .AddSingleton<IBusinessTypeValidator, BusinessTypeValidator>()
            .AddSingleton<IReceiverValidator, CalculationResponsibleReceiverValidator>()
            .AddScoped<IIncomingMessageReceiver, IncomingMessageReceiver>()
            .AddSingleton<IResponseFactory, JsonResponseFactory>()
            .AddSingleton<IResponseFactory, XmlResponseFactory>()
            .AddSingleton<ResponseFactory>();

        /*
        // Incomming Messages Publisher
        */
        services
            .AddScoped<IncomingMessagePublisher>();

        // => Service Bus
        services
            .AddOptions<IncomingMessagesQueueOptions>()
            .BindConfiguration(IncomingMessagesQueueOptions.SectionName)
            .ValidateDataAnnotations();

        var incommingMessagesQueueOptions =
            configuration
                .GetRequiredSection(IncomingMessagesQueueOptions.SectionName)
                .Get<IncomingMessagesQueueOptions>()
            ?? throw new InvalidOperationException("Missing Incomming Messages configuration.");

        services.AddAzureClients(builder =>
        {
            builder
                .AddClient<ServiceBusSender, ServiceBusClientOptions>((_, _, provider) =>
                    provider
                        .GetRequiredService<ServiceBusClient>()
                        .CreateSender(incommingMessagesQueueOptions.QueueName))
                .WithName(incommingMessagesQueueOptions.QueueName);
        });

        // => Health checks
        var defaultAzureCredential = new DefaultAzureCredential();

        services
            .AddHealthChecks()
            .AddAzureServiceBusQueue(
                sp => sp.GetRequiredService<IOptions<ServiceBusNamespaceOptions>>().Value.FullyQualifiedNamespace,
                sp => sp.GetRequiredService<IOptions<IncomingMessagesQueueOptions>>().Value.QueueName,
                _ => defaultAzureCredential,
                name: incommingMessagesQueueOptions.QueueName)
            .AddServiceBusQueueDeadLetter(
                sp => sp.GetRequiredService<IOptions<ServiceBusNamespaceOptions>>().Value.FullyQualifiedNamespace,
                sp => sp.GetRequiredService<IOptions<IncomingMessagesQueueOptions>>().Value.QueueName,
                _ => defaultAzureCredential,
                "Dead-letter (incoming messages)",
                [HealthChecksConstants.StatusHealthCheckTag]);

        /*
        // RegisterSchemaProviders
        */
        services
            .AddSingleton<CimJsonSchemas>()
            .AddSingleton<CimXmlSchemas>()
            .AddSingleton<CimXmlSchemaProvider>()
            .AddSingleton<JsonSchemaProvider>();
        return services;
    }
}
