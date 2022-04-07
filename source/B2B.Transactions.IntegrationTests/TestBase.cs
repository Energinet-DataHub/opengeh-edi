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
using B2B.Transactions.Infrastructure.Configuration;
using B2B.Transactions.IntegrationTests.Fixtures;
using B2B.Transactions.IntegrationTests.TestDoubles;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace B2B.Transactions.IntegrationTests
{
    [Collection("IntegrationTest")]
    public class TestBase : IDisposable
    {
        private readonly DatabaseFixture _databaseFixture;
        private readonly IServiceProvider _serviceProvider;
        private bool _disposed;

        protected TestBase(DatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture;
            _databaseFixture.CleanupDatabase();

            var services = new ServiceCollection();
            CompositionRoot.Initialize(services)
                .AddDatabaseConnectionFactory(_databaseFixture.ConnectionString)
                .AddDatabaseContext(_databaseFixture.ConnectionString)
                .AddSystemClock(new SystemDateTimeProviderStub());
            _serviceProvider = services.BuildServiceProvider();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected T GetService<T>()
            where T : notnull
        {
            return _serviceProvider.GetRequiredService<T>();
        }

        protected OutboxMessage GetOutboxMessage<T>()
            where T : notnull
        {
            var sql = $"SELECT * FROM [b2b].[OutboxMessages] WHERE Type = '{typeof(T).FullName}'";
            return GetService<IDbConnectionFactory>().GetOpenConnection().QuerySingleOrDefault<OutboxMessage>(sql);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed == true)
            {
                return;
            }

            ((ServiceProvider)_serviceProvider).Dispose();
            _disposed = true;
        }
    }
}
