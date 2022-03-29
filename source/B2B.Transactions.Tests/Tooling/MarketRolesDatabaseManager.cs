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
using B2B.Transactions.DataAccess;
using Energinet.DataHub.Core.FunctionApp.TestCommon.Database;
using Energinet.DataHub.MarketRoles.ApplyDBMigrationsApp.Helpers;
using EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using Microsoft.EntityFrameworkCore;

namespace B2B.Transactions.Tests.Tooling
{
    public class MarketRolesDatabaseManager : SqlServerDatabaseManager<B2BContext>
    {
        public MarketRolesDatabaseManager()
            : base("MarketRoles")
        {
        }

        public override B2BContext CreateDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<B2BContext>();
            var dbContextOptions = optionsBuilder
                .UseSqlServer(ConnectionString, options => options.UseNodaTime());

            return new B2BContext(optionsBuilder.Options);
        }

        /// <summary>
        /// Upgrade database.
        /// Used from some tests to seed database.
        /// </summary>
        public void UpgradeDatabase()
        {
            var result = DefaultUpgrader.Upgrade(ConnectionString);
            if (result.Successful is false)
            {
                throw new InvalidOperationException("Database migration failed", result.Error);
            }
        }

        /// <summary>
        /// Creates the database schema using DbUp instead of a database context.
        /// </summary>
        protected override Task<bool> CreateDatabaseSchemaAsync(B2BContext context)
        {
            return Task.FromResult(CreateDatabaseSchema(context));
        }

        /// <summary>
        /// Creates the database schema using DbUp instead of a database context.
        /// </summary>
        protected override bool CreateDatabaseSchema(B2BContext context)
        {
            UpgradeDatabase();
            return true;
        }
    }
}
