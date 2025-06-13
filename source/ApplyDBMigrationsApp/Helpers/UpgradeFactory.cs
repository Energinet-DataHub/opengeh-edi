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

using System.Reflection;
using DbUp;
using DbUp.Engine;
using Energinet.DataHub.EDI.ApplyDBMigrationsApp.Extensibility.DbUp;

namespace Energinet.DataHub.EDI.ApplyDBMigrationsApp.Helpers;

internal static class UpgradeFactory
{
    public static UpgradeEngine GetUpgradeEngine(
        string connectionString,
        string environment,
        bool isDryRun = false)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string must have a value");
        }

        EnsureDatabase.For.SqlDatabase(connectionString);

        var builder = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptNameComparer(new ScriptComparer())
            .WithScripts(new CustomScriptProvider(Assembly.GetExecutingAssembly(), GetScriptFilter(environment)))
            .LogToConsole();

        builder.Configure(configure => configure.ScriptExecutor.ExecutionTimeoutSeconds = 5 * 60); // 5 minutes timeout

        if (isDryRun)
        {
            builder.WithTransactionAlwaysRollback();
        }
        else
        {
            builder.WithTransaction();
        }

        return builder.Build();
    }

    private static Func<string, bool> GetScriptFilter(string environment)
    {
        if (environment.Contains("DEV") || environment.Contains("TEST"))
        {
            // In DEV and TEST environments we want to apply an additional script
            return file =>
                file.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)
                && (
                    file.Contains("202512061200 Grant access to query execution plan", StringComparison.OrdinalIgnoreCase)
                    || file.Contains(".Scripts.Model.", StringComparison.OrdinalIgnoreCase));
        }

        return file =>
            file.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)
            && file.Contains(".Scripts.Model.", StringComparison.OrdinalIgnoreCase);
    }
}
