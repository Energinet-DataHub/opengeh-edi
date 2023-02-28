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

using Application.IncomingMessages.RequestChangeCustomerCharacteristics;
using Application.IncomingMessages.RequestChangeOfSupplier;
using CimMessageAdapter.Messages;
using CimMessageAdapter.Messages.RequestChangeCustomerCharacteristics;
using CimMessageAdapter.Messages.RequestChangeOfSupplier;
using CimMessageAdapter.Response;
using DocumentValidation;
using Infrastructure.IncomingMessages.RequestChangeOfSupplier;
using Infrastructure.IncomingMessages.Response;
using Microsoft.Extensions.DependencyInjection;
using MarketActivityRecord = Application.IncomingMessages.RequestChangeOfSupplier.MarketActivityRecord;
using MessageParser = CimMessageAdapter.Messages.RequestChangeOfSupplier.MessageParser;
using SenderAuthorizer = CimMessageAdapter.Messages.RequestChangeOfSupplier.SenderAuthorizer;

namespace Infrastructure.Configuration;

internal static class IncomingMessageParsingServices
{
    internal static void AddIncomingMessageParsingServices(IServiceCollection services)
    {
        RegisterB2BResponseServices(services);
        RegisterSchemaProviders(services);
        RegisterRequestChangeOfSupplierMessageHandling(services);
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
    }

    private static void RegisterRequestChangeOfSupplierMessageHandling(IServiceCollection services)
    {
        services.AddScoped<SenderAuthorizer>();
        services.AddScoped<RequestChangeOfSupplierReceiver>();
        services
            .AddTransient<IMessageParser<MarketActivityRecord, RequestChangeOfSupplierTransaction>, JsonMessageParser>();
        services
            .AddTransient<IMessageParser<MarketActivityRecord, RequestChangeOfSupplierTransaction>, XmlMessageParser>();
        services.AddTransient<MessageParser>();
    }
}
