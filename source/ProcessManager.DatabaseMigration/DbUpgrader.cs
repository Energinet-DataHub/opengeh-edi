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

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using DbUp;
using DbUp.Engine;
using Microsoft.Data.SqlClient;

namespace Energinet.DataHub.ProcessManager.DatabaseMigration;

public static class DbUpgrader
{
    public static DatabaseUpgradeResult DatabaseUpgrade(string connectionString)
    {
        EnsureDatabase.For.SqlDatabase(connectionString);

        // We create the schema in code to ensure we can create the 'SchemaVersions'
        // table within the schema.
        var schemaName = "pm";
        CreateSchema(connectionString, schemaName);

        var upgrader =
            DeployChanges.To
                .SqlDatabase(connectionString)
                .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
                .LogToConsole()
                .JournalToSqlTable(schemaName, "SchemaVersions")
                .Build();

        var result = upgrader.PerformUpgrade();
        return result;
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "SQL doesn't contain user input")]
    private static void CreateSchema(string connectionString, string schemaName)
    {
        var createProcessManagerSchemaSql = $@"
            IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = '{schemaName}')
            BEGIN
                EXEC('CREATE SCHEMA {schemaName}');
            END";

        // Execute the pre-deployment script to create the schema if it doesn't exist
        using var connection = new SqlConnection(connectionString);
        connection.Open();

        using var command = new SqlCommand(createProcessManagerSchemaSql, connection);
        command.ExecuteNonQuery();
    }
}
