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

using Energinet.DataHub.EDI.Application.IncomingMessages.RequestAggregatedMeasureData;
using Energinet.DataHub.EDI.Application.IncomingMessages.RequestChangeCustomerCharacteristics;
using Energinet.DataHub.EDI.Application.IncomingMessages.RequestChangeOfSupplier;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.RequestAggregatedMeasureData;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.RequestChangeCustomerCharacteristics;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.RequestChangeOfSupplier;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Response;
using Energinet.DataHub.EDI.Infrastructure.DocumentValidation;
using Energinet.DataHub.EDI.Infrastructure.DocumentValidation.CimXml;
using Energinet.DataHub.EDI.Infrastructure.IncomingMessages.RequestChangeOfSupplier;
using Energinet.DataHub.EDI.Infrastructure.IncomingMessages.Response;
using Microsoft.Extensions.DependencyInjection;
using MarketActivityRecord = Energinet.DataHub.EDI.Application.IncomingMessages.RequestChangeOfSupplier.MarketActivityRecord;
using MessageParser = Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.RequestChangeOfSupplier.MessageParser;
using SenderAuthorizer = Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.RequestChangeOfSupplier.SenderAuthorizer;

namespace Energinet.DataHub.EDI.Infrastructure.Configuration;

internal static class IncomingMessageParsingServices
{
    internal static void AddIncomingMessageParsingServices(IServiceCollection services)
    {
        RegisterB2BResponseServices(services);
        RegisterSchemaProviders(services);
        RegisterRequestChangeOfSupplierMessageHandling(services);
        RegisterRequestAggregatedMeasureDataHandling(services);
        RegisterRequestChangeOfCustomerCharacteristicsMessageHandling(services);
    }

    private static void RegisterB2BResponseServices(IServiceCollection services)
    {
        services.AddSingleton<IResponseFactory, JsonResponseFactory>();
        services.AddSingleton<IResponseFactory, XmlResponseFactory>();
        services.AddSingleton<ResponseFactory>();
    }

    private static void RegisterSchemaProviders(IServiceCollection services)
    {
        services.AddSingleton<CimJsonSchemas>();
        services.AddSingleton<CimXmlSchemaProvider>();
        services.AddSingleton<JsonSchemaProvider>();
    }

    private static void RegisterRequestChangeOfCustomerCharacteristicsMessageHandling(IServiceCollection services)
    {
        services.AddScoped<CimMessageAdapter.Messages.RequestChangeCustomerCharacteristics.SenderAuthorizer>();
        services.AddScoped<RequestChangeCustomerCharacteristicsReceiver>();
        services
            .AddTransient<IMessageParser<Application.IncomingMessages.RequestChangeCustomerCharacteristics.
                    MarketActivityRecord, RequestChangeCustomerCharacteristicsTransaction>,
                IncomingMessages.RequestChangeCustomerCharacteristics.XmlMessageParser>();
        services.AddTransient<CimMessageAdapter.Messages.RequestChangeCustomerCharacteristics.MessageParser>();
        services.AddScoped<DefaultProcessTypeValidator>();
        services.AddScoped<DefaultMessageTypeValidator>();
        services.AddScoped<MasterDataReceiverResponsibleVerification>();
    }

    private static void RegisterRequestChangeOfSupplierMessageHandling(IServiceCollection services)
    {
        services.AddScoped<SenderAuthorizer>();
        services.AddScoped<RequestChangeOfSupplierReceiver>();
        services
            .AddTransient<IMessageParser<MarketActivityRecord, RequestChangeOfSupplierTransactionCommand>, JsonMessageParser>();
        services
            .AddTransient<IMessageParser<MarketActivityRecord, RequestChangeOfSupplierTransactionCommand>, XmlMessageParser>();
        services.AddTransient<MessageParser>();
        services.AddScoped<DefaultProcessTypeValidator>();
        services.AddScoped<DefaultMessageTypeValidator>();
        services.AddScoped<MasterDataReceiverResponsibleVerification>();
    }

    private static void RegisterRequestAggregatedMeasureDataHandling(IServiceCollection services)
    {
        services
            .AddScoped<IMessageParser<Serie, RequestAggregatedMeasureDataTransactionCommand>, Infrastructure.IncomingMessages.RequestAggregatedMeasureData.XmlMessageParser>();
        services
            .AddScoped<IMessageParser<Serie, RequestAggregatedMeasureDataTransactionCommand>, Infrastructure.IncomingMessages.RequestAggregatedMeasureData.JsonMessageParser>();
        services.AddTransient<CimMessageAdapter.Messages.RequestAggregatedMeasureData.MessageParser>();
        services.AddTransient<RequestAggregatedMeasureDataReceiver>();
        services.AddTransient<CimMessageAdapter.Messages.RequestAggregatedMeasureData.SenderAuthorizer>();
        services.AddTransient<RequestAggregatedMeasureDataTransactionCommand>();
        services.AddScoped<ProcessTypeValidator>();
        services.AddScoped<MessageTypeValidator>();
        services.AddScoped<CalculationResponsibleReceiverVerification>();
    }
}
