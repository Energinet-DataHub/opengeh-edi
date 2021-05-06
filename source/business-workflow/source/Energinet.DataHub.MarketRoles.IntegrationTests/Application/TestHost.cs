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
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier;
using Energinet.DataHub.MarketRoles.Application.ChangeOfSupplier.Processing;
using Energinet.DataHub.MarketRoles.Application.Common.Processing;
using Energinet.DataHub.MarketRoles.Domain.Consumers;
using Energinet.DataHub.MarketRoles.Domain.EnergySuppliers;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints;
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using Energinet.DataHub.MarketRoles.Infrastructure.BusinessRequestProcessing;
using Energinet.DataHub.MarketRoles.Infrastructure.BusinessRequestProcessing.Pipeline;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess.AccountingPoints;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess.Consumers;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess.EnergySuppliers;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess.ProcessManagers;
using Energinet.DataHub.MarketRoles.Infrastructure.EDIMessaging.ENTSOE.CIM.ChangeOfSupplier;
using Energinet.DataHub.MarketRoles.Infrastructure.Outbox;
using Energinet.DataHub.MarketRoles.Infrastructure.Serialization;
using EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.MarketRoles.IntegrationTests.Application
{
    public class TestHost
    {
        protected TestHost()
        {
            var services = new ServiceCollection();

            var connectionString = Environment.GetEnvironmentVariable("MarketData_IntegrationTests_ConnectionString");
            services.AddDbContext<MarketRolesContext>(x =>
                x.UseSqlServer(connectionString, y => y.UseNodaTime()));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ISystemDateTimeProvider, SystemDateTimeProviderStub>();
            services.AddScoped<IAccountingPointRepository, AccountingPointRepository>();
            services.AddScoped<IEnergySupplierRepository, EnergySupplierRepository>();
            services.AddScoped<IProcessManagerRepository, ProcessManagerRepository>();
            services.AddScoped<IConsumerRepository, ConsumerRepository>();
            services.AddScoped<IJsonSerializer, JsonSerializer>();
            services.AddScoped<IOutbox, OutboxProvider>();
            services.AddSingleton<IOutboxMessageFactory, OutboxMessageFactory>();

            services.AddMediatR(new[]
            {
                typeof(RequestChangeOfSupplierHandler).Assembly,
            });

            // Busines process responders
            services.AddScoped<IBusinessProcessResponder<RequestChangeOfSupplier>, RequestChangeOfSupplierResponder>();

            // Business process pipeline
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehaviour<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehaviour<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(BusinessProcessResponderBehaviour<,>));

            ServiceProvider = services.BuildServiceProvider();
            Mediator = ServiceProvider.GetRequiredService<IMediator>();
            AccountingPointRepository = ServiceProvider.GetRequiredService<IAccountingPointRepository>();
            EnergySupplierRepository = ServiceProvider.GetRequiredService<IEnergySupplierRepository>();
            ProcessManagerRepository = ServiceProvider.GetRequiredService<IProcessManagerRepository>();
            ConsumerRepository = ServiceProvider.GetRequiredService<IConsumerRepository>();
            UnitOfWork = ServiceProvider.GetRequiredService<IUnitOfWork>();
            MarketRolesContext = ServiceProvider.GetRequiredService<MarketRolesContext>();
            SystemDateTimeProvider = ServiceProvider.GetRequiredService<ISystemDateTimeProvider>();
            Serializer = ServiceProvider.GetRequiredService<IJsonSerializer>();
            SystemDateTimeProvider = new SystemDateTimeProviderStub();
        }

        protected IServiceProvider ServiceProvider { get; }

        protected IMediator Mediator { get; }

        protected IAccountingPointRepository AccountingPointRepository { get; }

        protected IEnergySupplierRepository EnergySupplierRepository { get; }

        protected IConsumerRepository ConsumerRepository { get; }

        protected IProcessManagerRepository ProcessManagerRepository { get; }

        protected IUnitOfWork UnitOfWork { get; }

        protected ISystemDateTimeProvider SystemDateTimeProvider { get; }

        protected MarketRolesContext MarketRolesContext { get; }

        protected IJsonSerializer Serializer { get; }

        protected ProcessManagerRouter Router { get; }
    }
}
