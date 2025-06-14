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

using Energinet.DataHub.EDI.ApplyDBMigrationsApp.Helpers;

namespace Energinet.DataHub.EDI.ApplyDBMigrationsApp;

public static class Program
{
    public static int Main(string[] args)
    {
        // First argument must be the connection string
        var connectionString = ConnectionStringParser.Parse(args);
        // If environment is specified, it must be the second argument
        var environment = EnvironmentParser.Parse(args);
        var isDryRun = args.Contains("dryRun");

        Console.WriteLine($"Performing upgrade using parameter Environment={environment};");
        var upgradeResult = DbUpgradeRunner.RunDbUpgrade(
            connectionString,
            environment,
            isDryRun);

        return ResultReporter.ReportResult(upgradeResult);
    }
}
