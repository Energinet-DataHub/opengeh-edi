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
using System.Threading.Tasks;
using B2B.Transactions.Infrastructure.DataAccess;
using Energinet.DataHub.MarketRoles.ApplyDBMigrationsApp.Helpers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace B2B.Transactions.IntegrationTests.Fixtures
{
    public class DatabaseFixture : IDisposable, IAsyncLifetime
    {
        private readonly B2BContext _context;
        private bool _disposed;

        public DatabaseFixture()
        {
            var optionsBuilder = new DbContextOptionsBuilder<B2BContext>();
            optionsBuilder
                .UseSqlServer(ConnectionString, options => options.UseNodaTime());

            _context = new B2BContext(optionsBuilder.Options);
        }

        public string ConnectionString { get; } = @"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=B2BTransactions;Integrated Security=True;";

        public Task InitializeAsync()
        {
            CreateSchema();
            CleanupDatabase();
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            Dispose();
            return Task.CompletedTask;
        }

        public void CleanupDatabase()
        {
            var cleanupStatement =
                $"DELETE FROM [dbo].[Transactions] " +
                $"DELETE FROM [dbo].[MessageIds] " +
                $"DELETE FROM [dbo].[TransactionIds]" +
                $"DELETE FROM [b2b].[OutboxMessages]";

            _context.Database.ExecuteSqlRaw(cleanupStatement);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed == true)
            {
                return;
            }

            CleanupDatabase();
            _context.Dispose();
            _disposed = true;
        }

        private void CreateSchema()
        {
            DefaultUpgrader.Upgrade(ConnectionString);
        }
    }
}
