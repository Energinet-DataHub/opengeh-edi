﻿// Copyright 2020 Energinet DataHub A/S
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
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.EDI.MasterData.IntegrationTests.Fixture.Database;

//TODO: We have this manager defined in 5 separate files. Consider moving them.
public class EdiDatabaseManager : SqlServerDatabaseManager<DbContext>
{
    public EdiDatabaseManager()
        : base("MasterData.IntegrationTests")
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
            });

        return (TDatabaseContext)Activator.CreateInstance(typeof(TDatabaseContext), optionsBuilder.Options)!;
    }

    public void CleanupDatabase()
    {
        var cleanupStatement =
            $"DELETE FROM [dbo].[Actor]" +
            $"DELETE FROM [dbo].[GridAreaOwner]" +
            $"DELETE FROM [dbo].[ActorCertificate]" +
            $"DELETE FROM [dbo].[ProcessDelegation]";

        using var connection = new SqlConnection(ConnectionString);
        connection.Open();

        using (var command = new SqlCommand(cleanupStatement, connection))
        {
            command.ExecuteNonQuery();
        }

        connection.Close();
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