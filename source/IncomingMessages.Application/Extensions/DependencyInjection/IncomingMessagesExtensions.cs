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
using Energinet.DataHub.EDI.DataAccess.Extensions.DependencyInjection;
using Energinet.DataHub.EDI.IncomingMessages.Application.MessageParser;
using Energinet.DataHub.EDI.IncomingMessages.Application.MessageValidators;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Configuration.Options;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.DocumentValidation;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.DocumentValidation.CimXml;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Messages;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Messages.RequestAggregatedMeasureData;
using Energinet.DataHub.EDI.IncomingMessages.Infrastructure.Response;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AggregatedMeasureDataB2CJsonMessageParser = Energinet.DataHub.EDI.IncomingMessages.Application.MessageParser.AggregatedMeasureDataRequestMessageParsers.B2CJsonMessageParser;
using AggregatedMeasureDataJsonMessageParser = Energinet.DataHub.EDI.IncomingMessages.Application.MessageParser.AggregatedMeasureDataRequestMessageParsers.JsonMessageParser;
using AggregatedMeasureDataXmlMessageParser = Energinet.DataHub.EDI.IncomingMessages.Application.MessageParser.AggregatedMeasureDataRequestMessageParsers.XmlMessageParser;
using WholesaleSettlementJsonMessageParser = Energinet.DataHub.EDI.IncomingMessages.Application.MessageParser.WholesaleSettlementMessageParsers.JsonMessageParser;

namespace Energinet.DataHub.EDI.IncomingMessages.Application.Extensions.DependencyInjection;

public static class IncomingMessagesExtensions
{
    public static IServiceCollection AddIncomingMessagesModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ServiceBusClientOptions>()
            .Bind(configuration)
            .Validate(
                o => !string.IsNullOrEmpty(o.INCOMING_MESSAGES_QUEUE_NAME),
                "INCOMING_MESSAGES_QUEUE_NAME must be set")
            .Validate(
                o => !string.IsNullOrEmpty(o.SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_MANAGE),
                "SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_MANAGE must be set");

        var serviceBusOptions = configuration.Get<ServiceBusClientOptions>()!;
        services
            .AddServiceBus(configuration)
            .TryAddExternalDomainServiceBusQueuesHealthCheck(
                serviceBusOptions.SERVICE_BUS_CONNECTION_STRING_FOR_DOMAIN_RELAY_MANAGE!,
                serviceBusOptions.INCOMING_MESSAGES_QUEUE_NAME!)
            .AddDapperConnectionToDatabase(configuration)
            .AddScopedSqlDbContext<IncomingMessagesContext>(configuration)
            .AddScoped<IIncomingMessageClient, IncomingMessageClient>()
            .AddScoped<ITransactionIdRepository, TransactionIdRepository>()
            .AddScoped<IMessageIdRepository, MessageIdRepository>()
            .AddScoped<IMessageParser, AggregatedMeasureDataXmlMessageParser>()
            .AddScoped<IMessageParser, AggregatedMeasureDataJsonMessageParser>()
            .AddScoped<IMessageParser, AggregatedMeasureDataB2CJsonMessageParser>()
            .AddScoped<IMessageParser, WholesaleSettlementJsonMessageParser>()
            .AddScoped<MarketMessageParser>()
            .AddScoped<ISenderAuthorizer, SenderAuthorizer>()
            .AddScoped<IncomingMessagePublisher>()
            .AddScoped<RequestAggregatedMeasureDataMessageValidator>()
            .AddSingleton<IProcessTypeValidator, ProcessTypeValidator>()
            .AddSingleton<IMessageTypeValidator, MessageTypeValidator>()
            .AddSingleton<IBusinessTypeValidator, BusinessTypeValidator>()
            .AddSingleton<IReceiverValidator, CalculationResponsibleReceiverValidator>()
            .AddScoped<IIncomingMessageReceiver, IncomingMessageReceiver>()
            .AddSingleton<IResponseFactory, JsonResponseFactory>()
            .AddSingleton<IResponseFactory, XmlResponseFactory>()
            .AddSingleton<ResponseFactory>();

        //RegisterSchemaProviders
        services.AddSingleton<CimJsonSchemas>()
            .AddSingleton<CimXmlSchemaProvider>()
            .AddSingleton<JsonSchemaProvider>();

        return services;
    }
}
