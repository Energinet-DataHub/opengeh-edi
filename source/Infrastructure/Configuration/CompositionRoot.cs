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
using Energinet.DataHub.EDI.Application.Configuration;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.MessageBus.RemoteBusinessServices;
using Energinet.DataHub.EDI.Common.DateTime;
using Energinet.DataHub.EDI.Common.Serialization;
using Energinet.DataHub.EDI.Infrastructure.Configuration.Authentication;
using Energinet.DataHub.EDI.Infrastructure.Configuration.DataAccess;
using Energinet.DataHub.EDI.Infrastructure.Configuration.FeatureFlag;
using Energinet.DataHub.EDI.Infrastructure.Configuration.IntegrationEvents;
using Energinet.DataHub.EDI.Infrastructure.DataRetention;
using Energinet.DataHub.EDI.Infrastructure.InboxEvents;
using Energinet.DataHub.EDI.Infrastructure.Wholesale;
using MediatR.Registration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Energinet.DataHub.EDI.Infrastructure.Configuration
{
    public class CompositionRoot
    {
        private readonly IServiceCollection _services;

        private CompositionRoot(IServiceCollection services)
        {
            _services = services;
            services.AddSingleton<HttpClient>();
            services.AddSingleton<ISerializer, Serializer>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IFeatureFlagProvider, FeatureFlagProviderProvider>();

            AddMediatR();
            services.AddLogging();
            AddAuthenticatedActor();
            AddWholeSaleInBox();
            IntegrationEventsConfiguration.Configure(services);
            InboxEventsConfiguration.Configure(services);
            QueryHandlingConfiguration.Configure(services);
            DataRetentionConfiguration.Configure(services);
        }

        public static CompositionRoot Initialize(IServiceCollection services)
        {
            return new CompositionRoot(services);
        }

        public CompositionRoot AddSystemClock(ISystemDateTimeProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            _services.AddScoped(sp => provider);
            return this;
        }

        public CompositionRoot AddBearerAuthentication(TokenValidationParameters tokenValidationParameters)
        {
            _services.AddScoped(sp => new JwtTokenParser(tokenValidationParameters));
            return this;
        }

        public CompositionRoot AddCorrelationContext(Func<IServiceProvider, ICorrelationContext> action)
        {
            _services.AddScoped(action);
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

        private void AddRemoteBusinessService<TRequest, TReply>(string responseQueueName)
            where TRequest : class
            where TReply : class
        {
            _services.AddSingleton(provider =>
                new RemoteBusinessService<TRequest, TReply>(
                    provider.GetRequiredService<IRemoteBusinessServiceRequestSenderAdapter<TRequest>>(),
                    responseQueueName));
        }

        private void AddMediatR()
        {
            var configuration = new MediatRServiceConfiguration();
            ServiceRegistrar.AddRequiredServices(_services, configuration);
        }

        private void AddAuthenticatedActor()
        {
            _services.AddScoped<AuthenticatedActor>();
        }

        private void AddWholeSaleInBox()
        {
            WholesaleInboxConfiguration.Configure(_services);
        }
    }
}
