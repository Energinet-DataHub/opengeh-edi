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
using Energinet.DataHub.Core.Logging.RequestResponseMiddleware.Storage;
using MediatR;
using MediatR.Registration;
using Messaging.Application.Actors;
using Messaging.Application.Configuration;
using Messaging.Application.Configuration.Authentication;
using Messaging.Application.Configuration.DataAccess;
using Messaging.Application.OutgoingMessages;
using Messaging.Application.OutgoingMessages.Common;
using Messaging.Application.OutgoingMessages.Common.Reasons;
using Messaging.Application.OutgoingMessages.Peek;
using Messaging.Application.Transactions.AggregatedTimeSeries;
using Messaging.Application.Transactions.MoveIn;
using Messaging.CimMessageAdapter.Messages;
using Messaging.Domain.MasterData.MarketEvaluationPoints;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.Transactions.MoveIn;
using Messaging.Infrastructure.Actors;
using Messaging.Infrastructure.Configuration.Authentication;
using Messaging.Infrastructure.Configuration.DataAccess;
using Messaging.Infrastructure.Configuration.FeatureFlag;
using Messaging.Infrastructure.Configuration.MessageBus;
using Messaging.Infrastructure.Configuration.MessageBus.RemoteBusinessServices;
using Messaging.Infrastructure.Configuration.Processing;
using Messaging.Infrastructure.Configuration.Serialization;
using Messaging.Infrastructure.IncomingMessages;
using Messaging.Infrastructure.MasterData.MarketEvaluationPoints;
using Messaging.Infrastructure.OutgoingMessages;
using Messaging.Infrastructure.OutgoingMessages.AccountingPointCharacteristics;
using Messaging.Infrastructure.OutgoingMessages.CharacteristicsOfACustomerAtAnAp;
using Messaging.Infrastructure.OutgoingMessages.Common;
using Messaging.Infrastructure.OutgoingMessages.Common.Reasons;
using Messaging.Infrastructure.OutgoingMessages.ConfirmRequestChangeAccountingPointCharacteristics;
using Messaging.Infrastructure.OutgoingMessages.ConfirmRequestChangeOfSupplier;
using Messaging.Infrastructure.OutgoingMessages.Dequeue;
using Messaging.Infrastructure.OutgoingMessages.GenericNotification;
using Messaging.Infrastructure.OutgoingMessages.NotifyAggregatedMeasureData;
using Messaging.Infrastructure.OutgoingMessages.Peek;
using Messaging.Infrastructure.OutgoingMessages.RejectRequestChangeAccountingPointCharacteristics;
using Messaging.Infrastructure.OutgoingMessages.RejectRequestChangeOfSupplier;
using Messaging.Infrastructure.Transactions;
using Messaging.Infrastructure.Transactions.MoveIn;
using Messaging.Infrastructure.Transactions.UpdateCustomer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

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
            services.AddScoped(typeof(IMessageQueueDispatcher<>), typeof(MessageQueueDispatcher<>));
            services.AddScoped<IMoveInTransactionRepository, MoveInTransactionRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IOutgoingMessageStore, OutgoingMessageStore>();
            services.AddScoped<IFeatureFlagProvider, FeatureFlagProviderProvider>();

            AddMediatR();
            services.AddLogging();
            InternalCommandProcessing.Configure(_services);
            AddMessageGenerationServices();
            AddMasterDataServices();
            AddActorServices();
            AddProcessing();
            ReadModelHandlingConfiguration.AddReadModelHandling(services);
            UpdateCustomerMasterDataConfiguration.Configure(services);
            DequeueConfiguration.Configure(services);
            SendAggregatedTimeSeriesConfiguration.Configure(services);
        }

        public static CompositionRoot Initialize(IServiceCollection services)
        {
            return new CompositionRoot(services);
        }

        public CompositionRoot AddMessageBus(string connectionString)
        {
            _services.AddSingleton<ServiceBusClient>(_ => new ServiceBusClient(connectionString));
            _services.AddSingleton<IServiceBusSenderFactory, ServiceBusSenderFactory>();
            return this;
        }

        public CompositionRoot AddPeekConfiguration(IBundleConfiguration bundleConfiguration)
        {
            PeekConfiguration.Configure(_services, bundleConfiguration);
            return this;
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

        public CompositionRoot AddAuthentication(Func<IServiceProvider, IMarketActorAuthenticator>? authenticatorBuilder = null)
        {
            if (authenticatorBuilder is null)
            {
                _services.AddScoped<IMarketActorAuthenticator, MarketActorAuthenticator>();
            }
            else
            {
                _services.AddScoped(authenticatorBuilder);
            }

            return this;
        }

        public CompositionRoot AddDatabaseConnectionFactory(string connectionString)
        {
            if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));
            _services.AddScoped<IEdiDatabaseConnection>(_ => new SqlEdiDatabaseConnection(connectionString));
            return this;
        }

        public CompositionRoot AddCorrelationContext(Func<IServiceProvider, ICorrelationContext> action)
        {
            _services.AddScoped(action);
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

        public CompositionRoot AddMessagePublishing()
        {
            _services.AddSingleton<IActorLookup, ActorLookup>();
            _services.AddScoped<IOutgoingMessageStore, OutgoingMessageStore>();
            _services.AddScoped<OutgoingMessageEnqueuer>();
            return this;
        }

        public CompositionRoot AddMoveInServices(
            MoveInSettings settings,
            Func<IServiceProvider, IMoveInRequester>? addMoveInRequestService = null,
            Func<IServiceProvider, ICustomerMasterDataClient>? addCustomerMasterDataClient = null,
            Func<IServiceProvider, IMeteringPointMasterDataClient>? addMeteringPointMasterDataClient = null)
        {
            MoveInConfiguration.Configure(_services, settings, addMoveInRequestService, addCustomerMasterDataClient, addMeteringPointMasterDataClient);
            return this;
        }

        public CompositionRoot AddMessageParserServices()
        {
            IncomingMessageParsingServices.AddIncomingMessageParsingServices(_services);
            return this;
        }

        public CompositionRoot AddHttpClientAdapter(Func<IServiceProvider, IHttpClientAdapter> action)
        {
            _services.AddSingleton(action);
            return this;
        }

        public CompositionRoot AddRemoteBusinessService<TRequest, TReply>(string remoteRequestQueueName, string responseQueueName)
            where TRequest : class
            where TReply : class
        {
            _services.AddSingleton<IRemoteBusinessServiceRequestSenderAdapter<TRequest>>(provider =>
                new RemoteBusinessServiceRequestSenderAdapter<TRequest>(provider.GetRequiredService<ServiceBusClient>(), remoteRequestQueueName));
            AddRemoteBusinessService<TRequest, TReply>(responseQueueName);
            return this;
        }

        public CompositionRoot AddRemoteBusinessService<TRequest, TReply>(Func<IServiceProvider, IRemoteBusinessServiceRequestSenderAdapter<TRequest>> adapterBuilder, string responseQueueName)
            where TRequest : class
            where TReply : class
        {
            _services.AddSingleton(adapterBuilder);
            AddRemoteBusinessService<TRequest, TReply>(responseQueueName);
            return this;
        }

        public CompositionRoot AddRequestHandler<TRequestHandler>()
            where TRequestHandler : class
        {
            _services.AddTransient<TRequestHandler>();

            return this;
        }

        private void AddRemoteBusinessService<TRequest, TReply>(string responseQueueName)
            where TRequest : class
            where TReply : class
        {
            _services.AddSingleton(provider =>
                new RemoteBusinessService<TRequest, TReply>(
                    provider.GetRequiredService<IRemoteBusinessServiceRequestSenderAdapter<TRequest>>(),
                    responseQueueName));
        }

        private void AddMessageGenerationServices()
        {
            _services.AddScoped<DocumentFactory>();
            _services.AddScoped<IMessageWriter, ConfirmChangeOfSupplierXmlMessageWriter>();
            _services.AddScoped<IMessageWriter, ConfirmChangeOfSupplierJsonMessageWriter>();
            _services.AddScoped<IMessageWriter, RejectRequestChangeOfSupplierXmlMessageWriter>();
            _services.AddScoped<IMessageWriter, GenericNotificationMessageWriter>();
            _services.AddScoped<IMessageWriter, AccountingPointCharacteristicsMessageWriter>();
            _services.AddScoped<IMessageWriter, ConfirmRequestChangeAccountingPointCharacteristicsMessageWriter>();
            _services.AddScoped<IMessageWriter, RejectRequestChangeAccountingPointCharacteristicsMessageWriter>();
            _services.AddScoped<IMessageWriter, CharacteristicsOfACustomerAtAnApMessageWriter>();
            _services.AddScoped<IMessageWriter, RejectRequestChangeOfSupplierJsonMessageWriter>();
            _services.AddScoped<IMessageWriter, NotifyAggregatedMeasureDataMessageWriter>();
            _services.AddScoped<IValidationErrorTranslator, ValidationErrorTranslator>();
            _services.AddScoped<IMessageRecordParser, MessageRecordParser>();
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
            _services.AddTransient(typeof(IPipelineBehavior<,>), typeof(EnqueueOutgoingMessagesBehaviour<,>));
        }
    }
}
