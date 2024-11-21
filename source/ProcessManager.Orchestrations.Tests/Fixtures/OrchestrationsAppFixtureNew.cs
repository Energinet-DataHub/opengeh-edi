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
using Xunit.Abstractions;

namespace Energinet.DataHub.ProcessManager.Orchestrations.Tests.Fixtures;

/// <summary>
/// Support testing Process Manager Orchestrations app using default fixture configuration.
/// </summary>
public class OrchestrationsAppFixtureNew
    : IAsyncLifetime
{
    public OrchestrationsAppFixtureNew()
    {
        OrchestrationsAppManager = new OrchestrationsAppManager();
    }

    public OrchestrationsAppManager OrchestrationsAppManager { get; }

    public async Task InitializeAsync()
    {
        await OrchestrationsAppManager.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await OrchestrationsAppManager.DisposeAsync();
    }

    public void SetTestOutputHelper(ITestOutputHelper testOutputHelper)
    {
        OrchestrationsAppManager.SetTestOutputHelper(testOutputHelper);
    }
}
