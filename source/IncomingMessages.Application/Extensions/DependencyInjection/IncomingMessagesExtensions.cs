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
using Energinet.DataHub.Core.App.Common.Diagnostics.HealthChecks;
using Energinet.DataHub.Core.Messaging.Communication.Extensions.Builder;
using Energinet.DataHub.Core.Messaging.Communication.Extensions.DependencyInjection;
using Energinet.DataHub.Core.Messaging.Communication.Extensions.Options;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.DataAccess.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.IncomingMessages.Application.UseCases;
using Energinet.DataHub.EDI.IncomingMessages.Domain.MessageParsers;
using Energinet.DataHub.EDI.IncomingMessages.Domain.MessageParsers.RSM012;
using Energinet.DataHub.EDI.IncomingMessages.Domain.MessageParsers.RSM016;
using Energinet.DataHub.EDI.IncomingMessages.Domain.MessageParsers.RSM017;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Schemas.Cim.Json;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Schemas.Cim.Xml;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Schemas.Ebix;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.ProcessManager;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Repositories.MessageId;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Repositories.TransactionId;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Response;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Energinet.DataHub.ProcessManager.Client.Extensions.DependencyInjection;
using Energinet.DataHub.ProcessManager.Client.Extensions.Options;
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
            .AddScoped<ISenderAuthorizer, SenderAuthorizer>()
            .AddScoped<ValidateIncomingMessage>()
            .AddSingleton<IProcessTypeValidator, ProcessTypeValidator>()
            .AddSingleton<IMessageTypeValidator, MessageTypeValidator>()
            .AddSingleton<IBusinessTypeValidator, BusinessTypeValidator>()
            .AddSingleton<IReceiverValidator, CalculationResponsibleReceiverValidator>()
            .AddScoped<IIncomingMessageReceiver, IncomingMessageReceiver>()
            .AddSingleton<IResponseFactory, JsonResponseFactory>()
            .AddSingleton<IResponseFactory, XmlResponseFactory>()
            .AddSingleton<IResponseFactory, EbixResponseFactory>()
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

        var incomingMessagesQueueOptions =
            configuration
                .GetRequiredSection(IncomingMessagesQueueOptions.SectionName)
                .Get<IncomingMessagesQueueOptions>()
            ?? throw new InvalidOperationException("Missing Incoming Messages configuration.");

        services.AddAzureClients(builder =>
        {
            builder
                .AddClient<ServiceBusSender, ServiceBusClientOptions>((_, _, provider) =>
                    provider
                        .GetRequiredService<ServiceBusClient>()
                        .CreateSender(incomingMessagesQueueOptions.QueueName))
                .WithName(incomingMessagesQueueOptions.QueueName);
        });

        // => Health checks
        var defaultAzureCredential = new DefaultAzureCredential();

        services
            .AddHealthChecks()
            .AddAzureServiceBusQueue(
                sp => sp.GetRequiredService<IOptions<ServiceBusNamespaceOptions>>().Value.FullyQualifiedNamespace,
                sp => sp.GetRequiredService<IOptions<IncomingMessagesQueueOptions>>().Value.QueueName,
                _ => defaultAzureCredential,
                name: incomingMessagesQueueOptions.QueueName)
            .AddServiceBusQueueDeadLetter(
                sp => sp.GetRequiredService<IOptions<ServiceBusNamespaceOptions>>().Value.FullyQualifiedNamespace,
                sp => sp.GetRequiredService<IOptions<IncomingMessagesQueueOptions>>().Value.QueueName,
                _ => defaultAzureCredential,
                "Dead-letter (incoming messages)",
                [HealthChecksConstants.StatusHealthCheckTag])
            .AddAzureServiceBusTopic(
                sp => sp.GetRequiredService<IOptions<ServiceBusNamespaceOptions>>().Value.FullyQualifiedNamespace,
                sp => sp.GetRequiredService<IOptions<ProcessManagerServiceBusClientOptions>>().Value.TopicName,
                tokenCredentialFactory: _ => defaultAzureCredential,
                name: "ProcessManager topic");

        /*
        // RegisterSchemaProviders
        */
        services
            .AddSingleton<CimJsonSchemas>()
            .AddSingleton<CimXmlSchemas>()
            .AddSingleton<CimXmlSchemaProvider>()
            .AddSingleton<EbixSchemaProvider>()
            .AddSingleton<JsonSchemaProvider>();

        services.AddTransient<IMessageParser, MeteredDateForMeasurementPointJsonMessageParser>();
        services.AddTransient<IMessageParser, MeteredDateForMeasurementPointEbixMessageParser>();
        services.AddTransient<IMessageParser, MeteredDateForMeasurementPointXmlMessageParser>();

        services.AddTransient<IMessageParser, WholesaleSettlementXmlMessageParser>();
        services.AddTransient<IMessageParser, WholesaleSettlementJsonMessageParser>();
        services.AddTransient<IMessageParser, WholesaleSettlementB2CJsonMessageParser>();

        services.AddTransient<IMessageParser, AggregatedMeasureDataXmlMessageParser>();
        services.AddTransient<IMessageParser, AggregatedMeasureDataJsonMessageParser>();
        services.AddTransient<IMessageParser, AggregatedMeasureDataB2CJsonMessageParser>();

        /*
         * Process Manager
         */
        services.AddTransient<IRequestProcessOrchestrationStarter, RequestProcessOrchestrationStarter>();
        services.AddTransient<MeteredDataOrchestrationStarter>();
        services.AddProcessManagerMessageClient();

        return services;
    }
}
