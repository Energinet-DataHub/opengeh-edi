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
using B2B.Transactions.Common;
using B2B.Transactions.Configuration;
using B2B.Transactions.Configuration.Authentication;
using B2B.Transactions.Configuration.DataAccess;
using B2B.Transactions.IncomingMessages;
using B2B.Transactions.Infrastructure.Authentication;
using B2B.Transactions.Infrastructure.Common;
using B2B.Transactions.Infrastructure.DataAccess;
using B2B.Transactions.Infrastructure.DataAccess.Transaction;
using B2B.Transactions.Infrastructure.InternalCommands;
using B2B.Transactions.Infrastructure.Messages;
using B2B.Transactions.Infrastructure.OutgoingMessages;
using B2B.Transactions.Infrastructure.Serialization;
using B2B.Transactions.Infrastructure.Transactions;
using B2B.Transactions.OutgoingMessages;
using B2B.Transactions.OutgoingMessages.ConfirmRequestChangeOfSupplier;
using B2B.Transactions.Transactions;
using B2B.Transactions.Xml.Incoming;
using Energinet.DataHub.Core.Logging.RequestResponseMiddleware.Storage;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using Energinet.DataHub.MessageHub.Client;
using Energinet.DataHub.MessageHub.Client.Factories;
using Energinet.DataHub.MessageHub.Client.Storage;
using Energinet.DataHub.MessageHub.Model.Peek;
using EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using MediatR;
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
            services.AddScoped<ITransactionQueueDispatcher, TransactionQueueDispatcher>();
            services.AddScoped<ITransactionRepository, TransactionRepository>();
            services.AddScoped<IMarketActorAuthenticator, MarketActorAuthenticator>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IOutgoingMessageStore, OutgoingMessageStore>();
            services.AddScoped<IncomingMessageHandler>();
            services.AddScoped<IMessageDispatcher, MessageDispatcher>();
            services.AddScoped<MessageFactory>();
            services.AddScoped<MessageRequestHandler>();
            services.AddScoped<IMarketActivityRecordParser, MarketActivityRecordParser>();
            services.AddScoped<MessageRequestContext>();

            services.AddLogging();
            AddXmlSchema(services);
            AddProcessing();
            AddInternalCommandsProcessing();
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

        public CompositionRoot AddMessagePublishing(INewMessageAvailableNotifier newMessageAvailableNotifier)
        {
            _services.AddScoped<INewMessageAvailableNotifier>(_ => newMessageAvailableNotifier);
            _services.AddScoped<MessagePublisher>();
            _services.AddScoped<IOutgoingMessageStore, OutgoingMessageStore>();
            return this;
        }

        public CompositionRoot AddOutgoingMessageDispatcher(IMessageDispatcher messageDispatcher)
        {
            _services.AddScoped<MessageFactory>();
            _services.AddScoped<IMessageDispatcher>(_ => messageDispatcher);
            _services.AddScoped<MessageRequestHandler>();

            return this;
        }

        public CompositionRoot AddMessageHubServices(string storageServiceConnectionString, string storageServiceContainerName)
        {
            if (storageServiceConnectionString == null) throw new ArgumentNullException(nameof(storageServiceConnectionString));
            if (storageServiceContainerName == null) throw new ArgumentNullException(nameof(storageServiceContainerName));
            _services.AddSingleton<StorageConfig>(s => new StorageConfig(storageServiceContainerName));
            _services.AddSingleton<IRequestBundleParser, RequestBundleParser>();
            _services.AddSingleton<IStorageServiceClientFactory>(s => new StorageServiceClientFactory(storageServiceConnectionString));
            _services.AddSingleton<IStorageHandler, StorageHandler>();

            return this;
        }

        private static void AddXmlSchema(IServiceCollection services)
        {
            services.AddScoped<SchemaStore>();
            services.AddScoped<ISchemaProvider, SchemaProvider>();
            services.AddScoped<MessageReceiver>();
        }

        private void AddProcessing()
        {
            _services.AddMediatR(typeof(CompositionRoot));
        }

        private void AddInternalCommandsProcessing()
        {
            _services.AddTransient<CommandExecutor>();
            _services.AddScoped<ICommandScheduler, CommandScheduler>();
            _services.AddTransient<InternalCommandAccessor>();
            _services.AddTransient<InternalCommandProcessor>();
        }
    }
}
