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

using System;
using Azure.Messaging.ServiceBus;
using B2B.CimMessageAdapter;
using B2B.CimMessageAdapter.Messages;
using B2B.CimMessageAdapter.Transactions;
using B2B.Transactions.Infrastructure.Authentication.Bearer;
using B2B.Transactions.Infrastructure.Authentication.MarketActors;
using B2B.Transactions.OutgoingMessages;
using B2B.Transactions.Xml.Incoming;
using B2B.Transactions.Xml.Outgoing;
using Energinet.DataHub.Core.Logging.RequestResponseMiddleware.Storage;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using Energinet.DataHub.MarketRoles.Infrastructure.Correlation;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess;
using Energinet.DataHub.MarketRoles.Infrastructure.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace B2B.Transactions.Infrastructure
{
    public class CompositionRoot
    {
        private readonly IServiceCollection _services;

        private CompositionRoot(IServiceCollection services)
        {
            _services = services;
        }

        public static CompositionRoot Initialize(IServiceCollection services)
        {
            return new CompositionRoot(services);
        }

        public static void BuildCompositionRoot(IServiceCollection services)
        {
            services.AddScoped<ISystemDateTimeProvider, SystemDateTimeProvider>();
            services.AddSingleton<IJsonSerializer, JsonSerializer>();
            services.AddScoped<SchemaStore>();
            services.AddScoped<ISchemaProvider, SchemaProvider>();
            services.AddScoped<MessageReceiver>();
            UseCorrelationContext(services);
            services.AddScoped<ITransactionIds, TransactionIdRegistry>();
            services.AddScoped<IMessageIds, MessageIdRegistry>();
            services.AddScoped<IDocumentProvider<IMessage>, AcceptDocumentProvider>();
            ConfigureTransactionQueue(services);
            services.AddScoped<ITransactionQueueDispatcher, TransactionQueueDispatcher>();
            services.AddLogging();

            ConfigureRequestLogging(services);
            ConfigureDatabaseConnectionFactory(services);
        }

        public CompositionRoot ConfigureAuthentication(TokenValidationParameters tokenValidationParameters)
        {
            _services.AddScoped<CurrentClaimsPrincipal>();
            _services.AddScoped<JwtTokenParser>(sp => new JwtTokenParser(tokenValidationParameters));
            _services.AddScoped<MarketActorAuthenticator>();
            return this;
        }

        private static void ConfigureDatabaseConnectionFactory(IServiceCollection services)
        {
            services.AddScoped<IDbConnectionFactory>(_ =>
            {
                var connectionString = Environment.GetEnvironmentVariable("MARKET_DATA_DB_CONNECTION_STRING");
                if (connectionString is null)
                {
                    throw new ArgumentNullException(connectionString);
                }

                return new SqlDbConnectionFactory(connectionString);
            });
        }

        private static void ConfigureRequestLogging(IServiceCollection services)
        {
            services.AddSingleton<IRequestResponseLogging>(s =>
            {
                var logger = services.BuildServiceProvider().GetService<ILogger<RequestResponseLoggingBlobStorage>>();
                var storage = new RequestResponseLoggingBlobStorage(
                    Environment.GetEnvironmentVariable("REQUEST_RESPONSE_LOGGING_CONNECTION_STRING") ?? throw new InvalidOperationException(), Environment.GetEnvironmentVariable("REQUEST_RESPONSE_LOGGING_CONTAINER_NAME") ?? throw new InvalidOperationException(), logger ?? throw new InvalidOperationException());
                return storage;
            });
        }

        private static void ConfigureTransactionQueue(IServiceCollection services)
        {
            services.AddSingleton<ServiceBusSender>(serviceProvider =>
            {
                var connectionString = Environment.GetEnvironmentVariable("MARKET_DATA_QUEUE_CONNECTION_STRING");
                var topicName = Environment.GetEnvironmentVariable("MARKET_DATA_QUEUE_NAME");
                return new ServiceBusClient(connectionString).CreateSender(topicName);
            });
        }

        private static void UseCorrelationContext(IServiceCollection services)
        {
            services.AddScoped<ICorrelationContext, CorrelationContext>(sp =>
            {
                var correlationContext = new CorrelationContext();
                if (IsRunningLocally())
                {
                    correlationContext.SetId(Guid.NewGuid().ToString());
                    correlationContext.SetParentId(Guid.NewGuid().ToString());
                }

                return correlationContext;
            });
        }

        private static bool IsRunningLocally()
        {
            return Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") == "Development";
        }
    }
}
