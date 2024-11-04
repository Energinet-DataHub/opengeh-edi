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

using Energinet.DataHub.Core.FunctionApp.TestCommon.Database;
using Energinet.DataHub.ProcessManagement.Core.Infrastructure.Database;
using Energinet.DataHub.ProcessManager.DatabaseMigration;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.ProcessManager.Core.Tests.Fixtures;

public class ProcessManagerDatabaseManager(string name)
    : SqlServerDatabaseManager<ProcessManagerContext>(name + $"_{DateTime.Now:yyyyMMddHHmm}_")
{
    public new string ConnectionString
    {
        get
        {
            var dbConnectionString = base.ConnectionString;
            if (!dbConnectionString.Contains("Trust")) // Trust Server Certificate is required for some tests
                dbConnectionString = $"{dbConnectionString};Trust Server Certificate=True;";
            return dbConnectionString;
        }
    }

    /// <inheritdoc/>
    public override ProcessManagerContext CreateDbContext() => CreateDbContext<ProcessManagerContext>();

    public TDatabaseContext CreateDbContext<TDatabaseContext>()
        where TDatabaseContext : DbContext
    {
        var optionsBuilder = new DbContextOptionsBuilder<TDatabaseContext>()
            .UseSqlServer(ConnectionString, options =>
            {
                options.UseNodaTime();
                options.EnableRetryOnFailure();
            });

        return (TDatabaseContext)Activator.CreateInstance(typeof(TDatabaseContext), optionsBuilder.Options)!;
    }

    /// <summary>
    /// Creates the database schema using DbUp instead of a database context.
    /// </summary>
    protected override Task<bool> CreateDatabaseSchemaAsync(ProcessManagerContext context)
    {
        return Task.FromResult(CreateDatabaseSchema(context));
    }

    /// <summary>
    /// Creates the database schema using DbUp instead of a database context.
    /// </summary>
    protected override bool CreateDatabaseSchema(ProcessManagerContext context)
    {
        var result = DbUpgrader.DatabaseUpgrade(ConnectionString);
        return !result.Successful
            ? throw new Exception("Database migration failed", result.Error) : true;
    }
}
