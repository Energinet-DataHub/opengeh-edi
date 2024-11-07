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

using Energinet.DataHub.ProcessManager.Core.Tests.Fixtures;

namespace Energinet.DataHub.ProcessManager.Client.Tests.Fixtures;

/// <summary>
/// Responsible for coordinating the configuration of app fixtures
/// <see cref="ScenarioOrchestrationsAppFixture"/> and
/// <see cref="ScenarioProcessManagerAppFixture"/>.
///
/// The two applications involved must:
/// - Use the same Database
/// - Use the same Task Hub
/// - Run on different ports
/// </summary>
public class ScenarioAppFixturesConfiguration
{
    private static readonly Lazy<ScenarioAppFixturesConfiguration> _instance =
        new(() => new ScenarioAppFixturesConfiguration());

    private ScenarioAppFixturesConfiguration()
    {
        DatabaseManager = new ProcessManagerDatabaseManager("ClientsTest");
        TaskHubName = "ClientsTest01";
        OrchestrationsAppPort = 8101;
        ProcessManagerAppPort = 8102;
    }

    public static ScenarioAppFixturesConfiguration Instance => _instance.Value;

    public ProcessManagerDatabaseManager DatabaseManager { get; }

    public string TaskHubName { get; }

    public int OrchestrationsAppPort { get; }

    public int ProcessManagerAppPort { get; }
}
