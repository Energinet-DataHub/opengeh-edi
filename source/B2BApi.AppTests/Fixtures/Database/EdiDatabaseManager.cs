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

namespace Energinet.DataHub.EDI.B2BApi.AppTests.Fixtures.Database;

public class EdiDatabaseManager : SqlServerDatabaseManager<DbContext>
{
    public EdiDatabaseManager()
        : base("Edi")
    {
    }

    /// <inheritdoc/>
    public override DbContext CreateDbContext() => CreateDbContext<DbContext>();

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

    public async Task AddActorAsync(ActorNumber actorNumber, string externalId)
    {
        await using var sqlConnection = new Microsoft.Data.SqlClient.SqlConnection(ConnectionString);

        await using var sqlCommand = sqlConnection.CreateCommand();
        sqlCommand.CommandText = "INSERT INTO [dbo].[Actor] VALUES (@id, @actorNumber, @externalId)";
        sqlCommand.Parameters.AddWithValue("@id", Guid.NewGuid());
        sqlCommand.Parameters.AddWithValue("@actorNumber", actorNumber.Value);
        sqlCommand.Parameters.AddWithValue("@externalId", externalId);

        await sqlConnection.OpenAsync();
        await sqlCommand.ExecuteNonQueryAsync();
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
