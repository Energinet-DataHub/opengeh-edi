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

using DbUp.Engine;
using Energinet.DataHub.EDI.ApplyDBMigrationsApp.Helpers;

namespace Energinet.DataHub.EDI.ApplyDBMigrationsApp;

public static class DbUpgradeRunner
{
    private static bool _isRunning;

    public static DatabaseUpgradeResult RunDbUpgrade(
        string connectionString,
        string environment = "",
        bool isDryRun = false)
    {
        while (_isRunning)
        {
            //To avoid both database fixtures from performing the upgrade at the same time
            Thread.Sleep(2000);
        }

        _isRunning = true;
        var upgrader = UpgradeFactory.GetUpgradeEngine(
            connectionString,
            environment,
            isDryRun);

        var result = upgrader.PerformUpgrade();

        _isRunning = false;
        return result;
    }
}
