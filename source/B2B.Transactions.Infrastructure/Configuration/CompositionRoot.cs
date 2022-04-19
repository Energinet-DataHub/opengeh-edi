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
using B2B.Transactions.Authentication;
using B2B.Transactions.Configuration;
using B2B.Transactions.DataAccess;
using B2B.Transactions.Infrastructure.Authentication.Bearer;
using B2B.Transactions.Infrastructure.Authentication.MarketActors;
using B2B.Transactions.Infrastructure.Configuration.Correlation;
using B2B.Transactions.Infrastructure.DataAccess;
using B2B.Transactions.Infrastructure.DataAccess.Transaction;
using B2B.Transactions.Infrastructure.Messages;
using B2B.Transactions.Infrastructure.OutgoingMessages;
using B2B.Transactions.Infrastructure.Serialization;
using B2B.Transactions.Infrastructure.Transactions;
using B2B.Transactions.OutgoingMessages;
using B2B.Transactions.Transactions;
using B2B.Transactions.Xml.Incoming;
using B2B.Transactions.Xml.Outgoing;
using Energinet.DataHub.Core.Logging.RequestResponseMiddleware.Storage;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using Energinet.DataHub.MessageHub.Client.DataAvailable;
using EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace B2B.Transactions.Infrastructure.Configuration
{
    public class CompositionRoot
    {
        private readonly IServiceCollection _services;

        private CompositionRoot(IServiceCollection services)
        {
            _services = services;
            services.AddSingleton<ISerializer, Serializer>();
            services.AddScoped<ITransactionIds, TransactionIdRegistry>();
            services.AddScoped<IMessageIds, MessageIdRegistry>();
            services.AddScoped<IMessageFactory<IDocument>, AcceptMessageFactory>();
            services.AddScoped<ITransactionQueueDispatcher, TransactionQueueDispatcher>();
            services.AddScoped<ITransactionRepository, TransactionRepository>();
            services.AddScoped<IMarketActorAuthenticator, MarketActorAuthenticator>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IOutgoingMessageStore, OutgoingMessageStore>();
            services.AddScoped<IMessageFactory<IDocument>, AcceptMessageFactory>();
            services.AddLogging();
            AddXmlSchema(services);
        }

        public static CompositionRoot Initialize(IServiceCollection services)
        {
            return new CompositionRoot(services);
        }

        public CompositionRoot AddDatabaseContext(string connectionString)
        {
            _services.AddDbContext<B2BContext>(x =>
            {
                x.UseSqlServer(connectionString, y => y.UseNodaTime());
            });
            return this;
        }

        public CompositionRoot AddSystemClock(ISystemDateTimeProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            _services.AddScoped(sp => provider);
            return this;
        }

        public CompositionRoot AddBearerAuthentication(TokenValidationParameters tokenValidationParameters)
        {
            _services.AddScoped<CurrentClaimsPrincipal>();
            _services.AddScoped(sp => new JwtTokenParser(tokenValidationParameters));
            return this;
        }

        public CompositionRoot AddDatabaseConnectionFactory(string connectionString)
        {
            if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));
            _services.AddScoped<IDbConnectionFactory>(_ => new SqlDbConnectionFactory(connectionString));
            return this;
        }

        public CompositionRoot AddCorrelationContext(Func<IServiceProvider, ICorrelationContext> action)
        {
            _services.AddScoped(action);
            return this;
        }

        public CompositionRoot AddTransactionQueue(string connectionString, string queueName)
        {
            _services.AddSingleton<ServiceBusSender>(serviceProvider => new ServiceBusClient(connectionString).CreateSender(queueName));
            return this;
        }

        public CompositionRoot AddRequestLogging(string blobStorageConnectionString, string storageContainerName)
        {
            if (blobStorageConnectionString == null) throw new ArgumentNullException(nameof(blobStorageConnectionString));
            if (storageContainerName == null) throw new ArgumentNullException(nameof(storageContainerName));
            _services.AddSingleton<IRequestResponseLogging>(s =>
            {
                var factory = s.GetService<ILoggerFactory>();
                var logger = factory.CreateLogger<RequestResponseLoggingBlobStorage>();
                return new RequestResponseLoggingBlobStorage(blobStorageConnectionString, storageContainerName, logger);
            });
            return this;
        }

        public CompositionRoot AddMessagePublishing(IDataAvailableNotificationSender dataAvailableNotificationSender)
        {
            _services.AddScoped<IDataAvailableNotificationSender>(_ => dataAvailableNotificationSender);
            _services.AddScoped<MessagePublisher>();
            _services.AddScoped<IOutgoingMessageStore, OutgoingMessageStore>();
            return this;
        }

        private static void AddXmlSchema(IServiceCollection services)
        {
            services.AddScoped<SchemaStore>();
            services.AddScoped<ISchemaProvider, SchemaProvider>();
            services.AddScoped<MessageReceiver>();
        }
    }
}
