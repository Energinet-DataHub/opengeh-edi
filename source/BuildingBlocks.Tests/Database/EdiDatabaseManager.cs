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
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Energinet.DataHub.BuildingBlocks.Tests.Database;

public class EdiDatabaseManager(string name) : SqlServerDatabaseManager<DbContext>(name + $"_{DateTime.Now:yyyyMMddHHmm}_")
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

    public void CleanupDatabase()
    {
         var cleanupStatement = """
                                DECLARE @sql NVARCHAR(MAX) = N'';

                                SELECT @sql += 'DELETE FROM ' + QUOTENAME(TABLE_SCHEMA) + '.' + QUOTENAME(TABLE_NAME) + ' '
                                FROM INFORMATION_SCHEMA.TABLES
                                WHERE TABLE_TYPE = 'BASE TABLE'
                                ORDER BY
                                    CASE
                                        WHEN TABLE_NAME = 'Bundles'
                                            THEN 0
                                        When TABLE_NAME = 'ActorMessageQueues'
                                            THEN 1
                                        WHEN TABLE_NAME LIKE '%ProcessGridAreas%'
                                            THEN 2                                        
                                        WHEN TABLE_NAME LIKE '%ProcessChargeTypes%'
                                            THEN 3
                                        WHEN TABLE_NAME LIKE '%Processes%'
                                            THEN 4
                                        ELSE 5
                                    END,
                                    TABLE_NAME
                                    ASC;    
                                select @sql += ';'

                                EXEC sp_executesql @sql;
                                """;

         using var connection = new SqlConnection(ConnectionString);
         connection.Open();

         using (var command = new SqlCommand(cleanupStatement, connection))
         {
             command.ExecuteNonQuery();
         }

         connection.Close();
    }

    public async Task AddActorAsync(ActorNumber actorNumber, string externalId)
    {
        await using var sqlConnection = new SqlConnection(ConnectionString);

        await using var sqlCommand = sqlConnection.CreateCommand();
        sqlCommand.CommandText = "INSERT INTO [dbo].[Actor] VALUES (@id, @actorNumber, @externalId)";
        sqlCommand.Parameters.AddWithValue("@id", Guid.NewGuid());
        sqlCommand.Parameters.AddWithValue("@actorNumber", actorNumber.Value);
        sqlCommand.Parameters.AddWithValue("@externalId", externalId);

        await sqlConnection.OpenAsync();
        await sqlCommand.ExecuteNonQueryAsync();
    }

    public async Task AddGridAreaOwnerAsync(ActorNumber actorNumber, string gridAreaCode)
    {
        await using var sqlConnection = new SqlConnection(ConnectionString);

        await using var sqlCommand = sqlConnection.CreateCommand();
        sqlCommand.CommandText = "INSERT INTO [dbo].[GridAreaOwner] VALUES (@id, @gridAreaCode, @validFrom, @gridAreaOwnerActorNumber, 0)";
        sqlCommand.Parameters.AddWithValue("@id", Guid.NewGuid());
        sqlCommand.Parameters.AddWithValue("@gridAreaOwnerActorNumber", actorNumber.Value);
        sqlCommand.Parameters.AddWithValue("@gridAreaCode", gridAreaCode);
        sqlCommand.Parameters.AddWithValue("@validFrom", SystemClock.Instance.GetCurrentInstant().Minus(Duration.FromDays(1)).ToDateTimeUtc());

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
