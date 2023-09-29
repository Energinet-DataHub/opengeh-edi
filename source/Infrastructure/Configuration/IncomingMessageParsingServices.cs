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

using Energinet.DataHub.EDI.Application.IncomingMessages;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Messages.RequestAggregatedMeasureData;
using Energinet.DataHub.EDI.Infrastructure.CimMessageAdapter.Response;
using Energinet.DataHub.EDI.Infrastructure.DocumentValidation;
using Energinet.DataHub.EDI.Infrastructure.DocumentValidation.CimXml;
using Energinet.DataHub.EDI.Infrastructure.IncomingMessages.Response;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddScoped<DefaultProcessTypeValidator>();
        services.AddScoped<DefaultMessageTypeValidator>();
        services.AddScoped<MasterDataReceiverResponsibleVerification>();
    }

    private static void RegisterRequestChangeOfSupplierMessageHandling(IServiceCollection services)
    {
        services.AddScoped<SenderAuthorizer>();
        services.AddScoped<DefaultProcessTypeValidator>();
        services.AddScoped<DefaultMessageTypeValidator>();
        services.AddScoped<MasterDataReceiverResponsibleVerification>();
    }

    private static void RegisterRequestAggregatedMeasureDataHandling(IServiceCollection services)
    {
        services
            .AddScoped<IMessageParser<RequestAggregatedMeasureDataMarketMessage>, IncomingMessages.RequestAggregatedMeasureData.XmlMessageParser>();
        services
            .AddScoped<IMessageParser<RequestAggregatedMeasureDataMarketMessage>, IncomingMessages.RequestAggregatedMeasureData.JsonMessageParser>();
        services.AddScoped<RequestAggregatedMeasureDataMarketMessageParser>();
        services.AddTransient<RequestAggregatedMeasureDataValidator>();
        services.AddTransient<SenderAuthorizer>();
        services.AddScoped<ProcessTypeValidator>();
        services.AddScoped<MessageTypeValidator>();
        services.AddScoped<CalculationResponsibleReceiverVerification>();
    }
}
