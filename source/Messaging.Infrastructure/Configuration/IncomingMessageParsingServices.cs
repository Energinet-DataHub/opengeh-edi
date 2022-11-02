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

using Messaging.Application.IncomingMessages.RequestChangeCustomerCharacteristics;
using Messaging.Application.IncomingMessages.RequestChangeOfSupplier;
using Messaging.Application.SchemaStore;
using Messaging.CimMessageAdapter.Messages;
using Messaging.CimMessageAdapter.Messages.RequestChangeOfSupplier;
using Messaging.CimMessageAdapter.Response;
using Messaging.Infrastructure.IncomingMessages.RequestChangeOfSupplier;
using Messaging.Infrastructure.IncomingMessages.Response;
using Microsoft.Extensions.DependencyInjection;
using MarketActivityRecord = Messaging.Application.IncomingMessages.RequestChangeOfSupplier.MarketActivityRecord;

namespace Messaging.Infrastructure.Configuration;

internal static class IncomingMessageParsingServices
{
    internal static void AddIncomingMessageParsingServices(IServiceCollection services)
    {
        RegisterRequestChangeOfSupplierParsers(services);

        services
            .AddTransient<IMessageParser<Application.IncomingMessages.RequestChangeCustomerCharacteristics.
                MarketActivityRecord, RequestChangeCustomerCharacteristicsTransaction>, IncomingMessages.RequestChangeCustomerCharacteristics.XmlMessageParser>();
        services.AddTransient<CimMessageAdapter.Messages.RequestChangeCustomerCharacteristics.MessageParser>();

        services.AddSingleton<CimJsonSchemas>();
        services.AddSingleton<XmlSchemaProvider>();
        services.AddSingleton<JsonSchemaProvider>();

        services.AddSingleton<JsonResponseFactory>();
        services.AddSingleton<XmlResponseFactory>();
        services.AddSingleton<ResponseFactory>();
    }

    private static void RegisterRequestChangeOfSupplierParsers(IServiceCollection services)
    {
        services
            .AddTransient<IMessageParser<MarketActivityRecord, RequestChangeOfSupplierTransaction>, JsonMessageParser>();
        services
            .AddTransient<IMessageParser<MarketActivityRecord, RequestChangeOfSupplierTransaction>, XmlMessageParser>();
        services.AddTransient<MessageParser>();
    }
}
