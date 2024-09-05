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
using Energinet.DataHub.EDI.ApplyDBMigrationsApp.Helpers;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.EDI.IntegrationTests;

public class EdiDatabaseManager : SqlServerDatabaseManager<DbContext>
{
    public EdiDatabaseManager()
        : base("EdiIntegrationTests")
    {
    }

    /// <inheritdoc/>
    public override DbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<DbContext>()
            .UseSqlServer(ConnectionString, options =>
            {
                options.UseNodaTime();
                options.EnableRetryOnFailure();
            });

        return (DbContext)Activator.CreateInstance(typeof(DbContext), optionsBuilder.Options)!;
    }

    /// <summary>
    /// Creates the database schema using DbUp instead of a database context.
    /// </summary>
    protected override Task<bool> CreateDatabaseSchemaAsync(DbContext context)
    {
        return Task.FromResult(CreateDatabaseSchema(context));
    }

    /// <summary>
    /// Creates the database schema using DbUp instead of a database context.
    /// </summary>
    protected override bool CreateDatabaseSchema(DbContext context)
    {
        var result = DbUpgradeRunner.RunDbUpgrade(ConnectionString);
        return !result.Successful ? throw new Exception("Database migration failed", result.Error) : true;
    }
}
