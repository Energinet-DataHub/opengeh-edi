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

namespace Energinet.DataHub.EDI.ApplyDBMigrationsApp.Helpers;

internal static class EnvironmentParser
{
    private static readonly string[] _validEnvironments = [
        "TEST-001",
        "TEST-002",
        "PREPROD-001",
        "PREPROD-002",
        "PROD-001",
        "SANDBOX-002",
        "DEV-001",
        "DEV-002",
        "DEV-003"];

    public static Func<string, bool> GetScriptFilter(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args, nameof(args));

        var environment = GetEnvironmentArgument(args);
        if (environment.Contains("DEV") || environment.Contains("TEST"))
        {
            // In DEV and TEST environments we want to apply an additional script
            return file =>
                file.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)
                && (
                    file.Contains("202512061200 Grant execution plan rights", StringComparison.OrdinalIgnoreCase)
                    || file.Contains(".Scripts.Model.", StringComparison.OrdinalIgnoreCase));
        }

        return file =>
            file.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)
            && file.Contains(".Scripts.Model.", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetEnvironmentArgument(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        return args.Count > 1 && _validEnvironments.Contains(args[1].ToUpperInvariant())
            ? args[1].ToUpperInvariant()
            : string.Empty;
    }
}
