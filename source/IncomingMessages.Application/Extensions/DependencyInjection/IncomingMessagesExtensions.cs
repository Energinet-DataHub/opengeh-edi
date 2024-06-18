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
using BuildingBlocks.Application.Extensions.Options;
using Energinet.DataHub.EDI.DataAccess.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.IncomingMessages.Application.UseCases;
using Energinet.DataHub.EDI.IncomingMessages.Domain.Validation;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParser;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParser.AggregatedMeasureDataRequestMessageParsers;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageParser.WholesaleSettlementMessageParsers;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.MessageRegistration;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Response;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Schemas.Cim.Json;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Schemas.Cim.Xml;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.IncomingMessages.Application.Extensions.DependencyInjection;

public static class IncomingMessagesExtensions
{
    public static IServiceCollection AddIncomingMessagesModule(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // Options
        services
            .AddOptions<IncomingMessagesQueueOptions>()
            .BindConfiguration(IncomingMessagesQueueOptions.SectionName)
            .ValidateDataAnnotations();
        services.AddOptions<ServiceBusOptions>()
            .BindConfiguration(ServiceBusOptions.SectionName)
            .ValidateDataAnnotations();

        services
            .AddFeatureFlags()
            .AddServiceBus(configuration)
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
            .AddScoped<IncomingMessagePublisher>()
            .AddScoped<ValidateIncomingMessage>()
            .AddSingleton<IProcessTypeValidator, ProcessTypeValidator>()
            .AddSingleton<IMessageTypeValidator, MessageTypeValidator>()
            .AddSingleton<IBusinessTypeValidator, BusinessTypeValidator>()
            .AddSingleton<IReceiverValidator, CalculationResponsibleReceiverValidator>()
            .AddScoped<IIncomingMessageReceiver, IncomingMessageReceiver>()
            .AddSingleton<IResponseFactory, JsonResponseFactory>()
            .AddSingleton<IResponseFactory, XmlResponseFactory>()
            .AddSingleton<ResponseFactory>();

        //RegisterSchemaProviders
        services
            .AddSingleton<CimJsonSchemas>()
            .AddSingleton<CimXmlSchemas>()
            .AddSingleton<CimXmlSchemaProvider>()
            .AddSingleton<JsonSchemaProvider>()

            // Health checks
            .TryAddExternalDomainServiceBusQueuesHealthCheck(
                configuration.GetSection(ServiceBusOptions.SectionName).Get<ServiceBusOptions>()!.ListenConnectionString,
                configuration.GetSection(IncomingMessagesQueueOptions.SectionName).Get<IncomingMessagesQueueOptions>()!.QueueName);

        return services;
    }
}
