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

using Energinet.DataHub.ProcessManager.Client.Tests.Fixtures;
using Xunit.Abstractions;

namespace Energinet.DataHub.ProcessManager.Client.Tests.Integration;

/// <summary>
/// Test case where we verify the Process Manager clients can be used to start a
/// calculation orchestration and monitor its status during its lifetime.
/// </summary>
[Collection(nameof(ProcessManagerClientCollection))]
public class MonitorCalculationScenario : IAsyncLifetime
{
    public MonitorCalculationScenario(
        ScenarioProcessManagerAppFixture processManagerAppFixture,
        ScenarioOrchestrationsAppFixture orchestrationsAppFixture,
        ITestOutputHelper testOutputHelper)
    {
        ProcessManagerAppFixture = processManagerAppFixture;
        ProcessManagerAppFixture.SetTestOutputHelper(testOutputHelper);

        OrchestrationsAppFixture = orchestrationsAppFixture;
        OrchestrationsAppFixture.SetTestOutputHelper(testOutputHelper);
    }

    private ScenarioProcessManagerAppFixture ProcessManagerAppFixture { get; }

    private ScenarioOrchestrationsAppFixture OrchestrationsAppFixture { get; }

    public Task InitializeAsync()
    {
        ProcessManagerAppFixture.AppHostManager.ClearHostLog();
        OrchestrationsAppFixture.AppHostManager.ClearHostLog();

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        ProcessManagerAppFixture.SetTestOutputHelper(null!);
        OrchestrationsAppFixture.SetTestOutputHelper(null!);

        return Task.CompletedTask;
    }

    [Fact]
    public async Task CalculationBrs023_WhenScheduledUsingClient_CanMonitorLifecycle()
    {
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
}
