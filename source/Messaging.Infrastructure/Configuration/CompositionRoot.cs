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
using System.Net.Http;
using Azure.Messaging.ServiceBus;
using Dapper;
using Energinet.DataHub.Core.Logging.RequestResponseMiddleware.Storage;
using Energinet.DataHub.MessageHub.Client;
using Energinet.DataHub.MessageHub.Client.DataAvailable;
using Energinet.DataHub.MessageHub.Client.Factories;
using Energinet.DataHub.MessageHub.Client.Peek;
using Energinet.DataHub.MessageHub.Client.Storage;
using Energinet.DataHub.MessageHub.Model.Peek;
using MediatR;
using MediatR.Registration;
using Messaging.Application.Actors;
using Messaging.Application.Configuration;
using Messaging.Application.Configuration.Authentication;
using Messaging.Application.Configuration.Commands;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.IncomingMessages.RequestChangeOfSupplier;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.OutgoingMessages.Common;
using Messaging.Application.OutgoingMessages.Common.Reasons;
using Messaging.Application.OutgoingMessages.Requesting;
using Messaging.Application.SchemaStore;
using Messaging.Application.Transactions.MoveIn;
using Messaging.Application.Transactions.MoveIn.MasterDataDelivery;
using Messaging.Application.Transactions.MoveIn.Notifications;
using Messaging.CimMessageAdapter;
using Messaging.CimMessageAdapter.Messages;
using Messaging.CimMessageAdapter.Messages.RequestChangeOfSupplier;
using Messaging.CimMessageAdapter.Response;
using Messaging.Domain.MasterData.MarketEvaluationPoints;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.Transactions.MoveIn;
using Messaging.Domain.Transactions.MoveIn.Events;
using Messaging.Infrastructure.Actors;
using Messaging.Infrastructure.Common;
using Messaging.Infrastructure.Common.Reasons;
using Messaging.Infrastructure.Configuration.Authentication;
using Messaging.Infrastructure.Configuration.DataAccess;
using Messaging.Infrastructure.Configuration.InternalCommands;
using Messaging.Infrastructure.Configuration.Processing;
using Messaging.Infrastructure.Configuration.Serialization;
using Messaging.Infrastructure.Configuration.SystemTime;
using Messaging.Infrastructure.IncomingMessages;
using Messaging.Infrastructure.MarketEvaluationPoints;
using Messaging.Infrastructure.MasterData.MarketEvaluationPoints;
using Messaging.Infrastructure.OutgoingMessages;
using Messaging.Infrastructure.OutgoingMessages.AccountingPointCharacteristics;
using Messaging.Infrastructure.OutgoingMessages.CharacteristicsOfACustomerAtAnAp;
using Messaging.Infrastructure.OutgoingMessages.ConfirmRequestChangeAccountingPointCharacteristics;
using Messaging.Infrastructure.OutgoingMessages.ConfirmRequestChangeOfSupplier;
using Messaging.Infrastructure.OutgoingMessages.GenericNotification;
using Messaging.Infrastructure.OutgoingMessages.RejectRequestChangeOfSupplier;
using Messaging.Infrastructure.OutgoingMessages.Requesting;
using Messaging.Infrastructure.Transactions;
using Messaging.Infrastructure.Transactions.MoveIn;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NodaTime;
using MarketActivityRecord = Messaging.Application.IncomingMessages.RequestChangeOfSupplier.MarketActivityRecord;

namespace Messaging.Infrastructure.Configuration
{
    public class CompositionRoot
    {
        private readonly IServiceCollection _services;

        private CompositionRoot(IServiceCollection services)
        {
            _services = services;
            services.AddSingleton<HttpClient>();
            services.AddSingleton<ISerializer, Serializer>();
            services.AddScoped<ITransactionIds, TransactionIdRegistry>();
            services.AddScoped<IMessageIds, MessageIdRegistry>();
            services.AddScoped<IMessageQueueDispatcher, MessageQueueDispatcher>();
            services.AddScoped<IMoveInTransactionRepository, MoveInTransactionRepository>();
            services.AddScoped<IMarketActorAuthenticator, MarketActorAuthenticator>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IOutgoingMessageStore, OutgoingMessageStore>();
            services.AddScoped<IMessageRequestNotifications, MessageRequestNotifications>();
            services.AddTransient<IRequestHandler<RequestMessages, Unit>, RequestMessagesHandler>();
            services.AddScoped<MessageReceiver>();

            AddMediatR();
            services.AddLogging();
            AddInternalCommandsProcessing();
            AddMessageGenerationServices();
            AddMasterDataServices();
            AddActorServices();
            AddProcessing();
            ReadModelHandlingConfiguration.AddReadModelHandling(services);
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

        public CompositionRoot AddIncomingMessageQueue(string connectionString, string queueName)
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
                var factory = s.GetRequiredService<ILoggerFactory>();
                var logger = factory.CreateLogger<RequestResponseLoggingBlobStorage>();
                return new RequestResponseLoggingBlobStorage(blobStorageConnectionString, storageContainerName, logger);
            });
            return this;
        }

        public CompositionRoot AddMessagePublishing(Func<IServiceProvider, INewMessageAvailableNotifier> action)
        {
            _services.AddScoped(action);
            _services.AddSingleton<IActorLookup, ActorLookup>();
            _services.AddScoped<MessageAvailabilityPublisher>();
            _services.AddScoped<IOutgoingMessageStore, OutgoingMessageStore>();
            _services.AddTransient<INotificationHandler<TimeHasPassed>, PublishNewMessagesOnTimeHasPassed>();
            return this;
        }

        public CompositionRoot AddMessageStorage(Func<IServiceProvider, IMessageStorage> action)
        {
            _services.AddSingleton(action);
            return this;
        }

        public CompositionRoot AddMessageHubServices(string storageServiceConnectionString, string storageServiceContainerName, string queueConnectionString, string dataAvailableQueue, string domainReplyQueue)
        {
            _services.AddSingleton<StorageConfig>(s => new StorageConfig(storageServiceContainerName));
            _services.AddSingleton<IRequestBundleParser, RequestBundleParser>();
            _services.AddSingleton<IResponseBundleParser, ResponseBundleParser>();
            _services.AddSingleton<IStorageServiceClientFactory>(s => new StorageServiceClientFactory(storageServiceConnectionString));
            _services.AddSingleton<IStorageHandler, StorageHandler>();

            _services.AddSingleton<IServiceBusClientFactory>(_ => new ServiceBusClientFactory(queueConnectionString));
            _services.AddSingleton<IMessageBusFactory, AzureServiceBusFactory>();
            _services.AddSingleton<IDataAvailableNotificationSender, DataAvailableNotificationSender>();
            _services.AddSingleton<IDataBundleResponseSender, DataBundleResponseSender>();
            _services.AddSingleton(_ => new MessageHubConfig(dataAvailableQueue, domainReplyQueue));
            _services.AddTransient<IRequestHandler<SendSuccessNotification, Unit>, SendSuccessNotificationHandler>();
            _services.AddTransient<IRequestHandler<SendFailureNotification, Unit>, SendFailureNotificationHandler>();

            return this;
        }

        public CompositionRoot AddRequestHandler<TRequestHandler>()
            where TRequestHandler : class
        {
            _services.AddTransient<TRequestHandler>();

            return this;
        }

        public CompositionRoot AddNotificationHandler<TNotificationHandler, TNotification>()
            where TNotificationHandler : class, INotificationHandler<TNotification>
            where TNotification : INotification
        {
            _services.AddTransient<INotificationHandler<TNotification>, TNotificationHandler>();

            return this;
        }

        public CompositionRoot AddMoveInServices(MoveInConfiguration configuration)
        {
            _services.AddScoped<MoveInNotifications>();
            _services.AddScoped(_ => configuration);
            _services.AddScoped<IMoveInRequester, MoveInRequester>();
            _services.AddScoped<IMeteringPointMasterDataClient, MeteringPointMasterDataClient>();
            _services.AddScoped<ICustomerMasterDataClient, CustomerMasterDataClient>();
            _services.AddTransient<IRequestHandler<RequestChangeOfSupplierTransaction, Unit>, MoveInRequestHandler>();
            _services.AddTransient<IRequestHandler<FetchCustomerMasterData, Unit>, FetchCustomerMasterDataHandler>();
            _services.AddTransient<IRequestHandler<FetchMeteringPointMasterData, Unit>, FetchMeteringPointMasterDataHandler>();
            _services.AddTransient<IRequestHandler<SetConsumerHasMovedIn, Unit>, SetConsumerHasMovedInHandler>();
            _services.AddTransient<IRequestHandler<ForwardMeteringPointMasterData, Unit>, ForwardMeteringPointMasterDataHandler>();
            _services.AddTransient<IRequestHandler<ForwardCustomerMasterData, Unit>, ForwardCustomerMasterDataHandler>();
            _services.AddTransient<IRequestHandler<NotifyCurrentEnergySupplier, Unit>, NotifyCurrentEnergySupplierHandler>();
            _services.AddTransient<IRequestHandler<NotifyGridOperator, Unit>, NotifyGridOperatorHandler>();
            _services.AddTransient<IRequestHandler<SendCustomerMasterDataToGridOperator, Unit>, SendCustomerMasterDataToGridOperatorHandler>();
            _services.AddTransient<IRequestHandler<SetCustomerMasterData, Unit>, SetCustomerMasterDataHandler>();
            _services.AddTransient<INotificationHandler<MoveInWasAccepted>, FetchMeteringPointMasterDataWhenAccepted>();
            _services.AddTransient<INotificationHandler<MoveInWasAccepted>, FetchCustomerMasterDataWhenAccepted>();
            _services.AddTransient<INotificationHandler<EndOfSupplyNotificationChangedToPending>, NotifyCurrentEnergySupplierWhenConsumerHasMovedIn>();
            _services.AddTransient<INotificationHandler<BusinessProcessWasCompleted>, NotifyGridOperatorWhenConsumerHasMovedIn>();
            return this;
        }

        public CompositionRoot AddMessageParserServices()
        {
            _services.AddSingleton(_ => new MessageParser(new IMessageParser<MarketActivityRecord, RequestChangeOfSupplierTransaction>[]
            {
                new JsonMessageParser(),
                new XmlMessageParser(),
            }));
            _services.AddSingleton<XmlMessageParser>();
            _services.AddSingleton<JsonMessageParser>();
            _services.AddSingleton<XmlSchemaProvider>();
            _services.AddSingleton<JsonSchemaProvider>();
            _services.AddSingleton(_ => new ResponseFactory(new IResponseFactory[]
            {
                new JsonResponseFactory(),
                new XmlResponseFactory(),
            }));
            return this;
        }

        public CompositionRoot AddHttpClientAdapter(Func<IServiceProvider, IHttpClientAdapter> action)
        {
            _services.AddSingleton(action);
            return this;
        }

        private void AddInternalCommandsProcessing()
        {
            _services.AddTransient<CommandExecutor>();
            _services.AddScoped<ICommandScheduler, CommandScheduler>();
            _services.AddScoped<CommandSchedulerFacade>();
            _services.AddTransient<InternalCommandAccessor>();
            _services.AddTransient<InternalCommandProcessor>();
            _services.AddTransient<INotificationHandler<TimeHasPassed>, ProcessInternalCommandsOnTimeHasPassed>();
        }

        private void AddMessageGenerationServices()
        {
            _services.AddScoped<DocumentFactory>();
            _services.AddScoped<IDocumentWriter, ConfirmChangeOfSupplierXmlDocumentWriter>();
            _services.AddScoped<IDocumentWriter, ConfirmChangeOfSupplierJsonDocumentWriter>();
            _services.AddScoped<IDocumentWriter, RejectRequestChangeOfSupplierXmlDocumentWriter>();
            _services.AddScoped<IDocumentWriter, GenericNotificationDocumentWriter>();
            _services.AddScoped<IDocumentWriter, AccountingPointCharacteristicsDocumentWriter>();
            _services.AddScoped<IDocumentWriter, ConfirmRequestChangeAccountingPointCharacteristicsDocumentWriter>();
            _services.AddScoped<IDocumentWriter, CharacteristicsOfACustomerAtAnApDocumentWriter>();
            _services.AddScoped<IDocumentWriter, RejectRequestChangeOfSupplierJsonDocumentWriter>();
            _services.AddScoped<IValidationErrorTranslator, ValidationErrorTranslator>();
            _services.AddScoped<IMarketActivityRecordParser, MarketActivityRecordParser>();
        }

        private void AddMediatR()
        {
            var configuration = new MediatRServiceConfiguration();
            ServiceRegistrar.AddRequiredServices(_services, configuration);
        }

        private void AddMasterDataServices()
        {
            _services.AddScoped<IMarketEvaluationPointRepository, MarketEvaluationPointRepository>();
        }

        private void AddActorServices()
        {
            _services.AddTransient<IRequestHandler<CreateActor, Unit>, CreateActorHandler>();
            _services.AddTransient<IActorRegistry, ActorRegistry>();
        }

        private void AddProcessing()
        {
            _services.AddScoped<DomainEventsAccessor>();
            _services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehaviour<,>));
            _services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RaiseDomainEventsBehaviour<,>));
        }
    }
}
