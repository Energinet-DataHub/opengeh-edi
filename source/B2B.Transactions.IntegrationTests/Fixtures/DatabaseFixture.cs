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
using B2B.Transactions.Infrastructure.DataAccess;
using Energinet.DataHub.MarketRoles.ApplyDBMigrationsApp.Helpers;
using EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using Microsoft.EntityFrameworkCore;

namespace B2B.Transactions.IntegrationTests.Fixtures
{
    public class DatabaseFixture : IDisposable
    {
        private readonly B2BContext _context;
        private bool _disposed;

        public DatabaseFixture(string connectionString)
        {
            ConnectionString = connectionString;
            var optionsBuilder = new DbContextOptionsBuilder<B2BContext>();
            optionsBuilder
                .UseSqlServer(connectionString, options => options.UseNodaTime());

            _context = new B2BContext(optionsBuilder.Options);
        }

        protected string ConnectionString { get; }

        public void Initialize()
        {
            InitializeDatabase();
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

        private void InitializeDatabase()
        {
            CreateDatabase();
            CreateSchema();
            CleanupDatabase();
        }

        private void CreateSchema()
        {
            DefaultUpgrader.Upgrade(ConnectionString);
        }

        private void CreateDatabase()
        {
            _context.Database.EnsureCreated();
        }

        private void CleanupDatabase()
        {
            var cleanupStatement =
                $"DELETE FROM [dbo].[Transactions] " +
                $"DELETE FROM [dbo].[MessageIds] " +
                $"DELETE FROM [dbo].[TransactionIds]";

            _context.Database.ExecuteSqlRaw(cleanupStatement);
        }
    }
}
