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

using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.Configuration;
using Energinet.DataHub.EDI.IncomingMessages.Interfaces;
using IncomingMessages.Infrastructure;
using IncomingMessages.Infrastructure.Configuration.DataAccess;
using IncomingMessages.Infrastructure.Messages;
using IncomingMessages.Infrastructure.Messages.RequestAggregatedMeasureData;
using IncomingMessages.Infrastructure.RequestAggregatedMeasureDataParsers;
using IncomingMessages.Infrastructure.Response;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.EDI.IncomingMessages.Application.Configuration;

public static class IncomingMessagesConfiguration
{
    public static void Configure(IServiceCollection services)
    {
        services.AddScopedSqlDbContext<IncomingMessagesContext>();
        services.AddScoped<IIncomingRequestAggregatedMeasuredData, IncomingRequestAggregatedMeasuredData>();
        services.AddScoped<ITransactionIdRepository, TransactionIdRepository>();
        services.AddScoped<IMessageIdRepository, MessageIdRepository>();
        services.AddScoped<IMessageParser, XmlMessageParser>();
        services.AddScoped<IMessageParser, JsonMessageParser>();
        services.AddScoped<IMessageParser, ProtoMessageParser>();
        services.AddScoped<RequestAggregatedMeasureDataMarketMessageParser>();
        services.AddTransient<SenderAuthorizer>();
        services.AddTransient<IncomingRequestAggregatedMeasuredDataSender>();
        services.AddTransient<RequestAggregatedMeasureDataValidator>();
        services.AddScoped<ProcessTypeValidator>();
        services.AddScoped<MessageTypeValidator>();
        services.AddScoped<BusinessTypeValidator>();
        services.AddScoped<CalculationResponsibleReceiverVerification>();
        services.AddSingleton<IResponseFactory, JsonResponseFactory>();
        services.AddSingleton<IResponseFactory, XmlResponseFactory>();
        services.AddSingleton<ResponseFactory>();
    }
}
