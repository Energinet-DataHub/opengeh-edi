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
using B2B.Transactions.DataAccess;
using B2B.Transactions.DataAccess.Transaction;
using B2B.Transactions.Transactions;
using EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace B2B.Transactions.Tests
{
#pragma warning disable CA1724 // TODO: TestHost is reserved. Maybe refactor to base EntryPoint?
    public class TestHost : IDisposable
    {
        private readonly Scope _scope;
        private readonly Container _container;
        private bool _disposed;

        protected TestHost()
        {
            var serviceCollection = new ServiceCollection();

            _container = new Container();
            _container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();

            var connectionString = "Server=localhost\\SQLEXPRESS;Database=MarketRolesTestDB;Trusted_Connection=True";
            serviceCollection.AddDbContext<MarketRolesContext>(x =>
                x.UseSqlServer(connectionString, y => y.UseNodaTime()));
            serviceCollection.AddSimpleInjector(_container);
            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider().UseSimpleInjector(_container);

            _container.Register<IDbConnectionFactory>(() => new SqlDbConnectionFactory(connectionString), Lifestyle.Scoped);

            _container.Register<ITransactionRepository, TransactionRepository>(Lifestyle.Scoped);

            _container.Verify();

            _scope = AsyncScopedLifestyle.BeginScope(_container);

            MarketRolesContext = _container.GetInstance<MarketRolesContext>();
            TransactionRepository = _container.GetInstance<ITransactionRepository>();
        }

        protected MarketRolesContext MarketRolesContext { get; }

        protected ITransactionRepository TransactionRepository { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _scope.Dispose();
            _container.Dispose();

            _disposed = true;
        }
    }
}
