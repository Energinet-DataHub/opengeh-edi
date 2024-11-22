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

using System.Dynamic;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Energinet.DataHub.Core.FunctionApp.TestCommon.FunctionAppHost;
using Energinet.DataHub.Core.TestCommon;
using Energinet.DataHub.ProcessManager.Api.Model.OrchestrationInstance;
using Energinet.DataHub.ProcessManager.Client.Tests.Fixtures;
using FluentAssertions;
using Xunit.Abstractions;

namespace Energinet.DataHub.ProcessManager.Client.Tests.Integration;

/// <summary>
/// Test case where we verify the Process Manager clients can be used to start a
/// calculation orchestration and monitor its status during its lifetime.
/// </summary>
[Collection(nameof(ProcessManagerClientCollection))]
public class MonitorCalculationUsingApiScenario : IAsyncLifetime
{
    public MonitorCalculationUsingApiScenario(
        ProcessManagerClientFixture processManagerClientFixture,
        ITestOutputHelper testOutputHelper)
    {
        ProcessManagerClientFixture = processManagerClientFixture;
        processManagerClientFixture.SetTestOutputHelper(testOutputHelper);
    }

    public ProcessManagerClientFixture ProcessManagerClientFixture { get; }

    public Task InitializeAsync()
    {
        ProcessManagerClientFixture.ProcessManagerAppManager.AppHostManager.ClearHostLog();
        ProcessManagerClientFixture.OrchestrationsAppManager.AppHostManager.ClearHostLog();

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        ProcessManagerClientFixture.SetTestOutputHelper(null);

        return Task.CompletedTask;
    }

    [Fact]
    public async Task CalculationBrs023_WhenScheduledUsingClient_CanMonitorLifecycle()
    {
        // TODO: Move to API test project
        dynamic scheduleRequestDto = new ExpandoObject();
        scheduleRequestDto.OperatingIdentity = new ExpandoObject();
        scheduleRequestDto.OperatingIdentity.UserId = Guid.NewGuid();
        scheduleRequestDto.OperatingIdentity.ActorId = Guid.NewGuid();
        scheduleRequestDto.RunAt = "2024-11-01T06:19:10.0209567+01:00";
        scheduleRequestDto.InputParameter = new ExpandoObject();
        scheduleRequestDto.InputParameter.CalculationType = 0;
        scheduleRequestDto.InputParameter.GridAreaCodes = new[] { "543" };
        scheduleRequestDto.InputParameter.PeriodStartDate = "2024-10-29T15:19:10.0151351+01:00";
        scheduleRequestDto.InputParameter.PeriodEndDate = "2024-10-29T16:19:10.0193962+01:00";
        scheduleRequestDto.InputParameter.IsInternalCalculation = true;

        using var scheduleRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/processmanager/orchestrationinstance/brs_023_027/1");
        scheduleRequest.Content = new StringContent(
            JsonSerializer.Serialize(scheduleRequestDto),
            Encoding.UTF8,
            "application/json");

        // Step 1: Schedule new calculation orchestration instance
        using var scheduleResponse = await ProcessManagerClientFixture.OrchestrationsAppManager.AppHostManager
            .HttpClient
            .SendAsync(scheduleRequest);
        scheduleResponse.EnsureSuccessStatusCode();

        var calculationId = await scheduleResponse.Content
            .ReadFromJsonAsync<Guid>();

        // Step 2: Trigger the scheduler to queue the calculation orchestration instance
        await ProcessManagerClientFixture.ProcessManagerAppManager.AppHostManager
            .TriggerFunctionAsync("StartScheduledOrchestrationInstances");

        // Step 3: Query until terminated with succeeded
        var isTerminated = await Awaiter.TryWaitUntilConditionAsync(
            async () =>
            {
                using var queryRequest = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"/api/processmanager/orchestrationinstance/{calculationId}");

                using var queryResponse = await ProcessManagerClientFixture.ProcessManagerAppManager.AppHostManager
                    .HttpClient
                    .SendAsync(queryRequest);
                queryResponse.EnsureSuccessStatusCode();

                var orchestrationInstance = await queryResponse.Content
                    .ReadFromJsonAsync<OrchestrationInstanceDto>();

                return orchestrationInstance!.Lifecycle.State == OrchestrationInstanceLifecycleStates.Terminated;
            },
            timeLimit: TimeSpan.FromSeconds(40),
            delay: TimeSpan.FromSeconds(2));

        isTerminated.Should().BeTrue("because we expects the orchestration instance can complete within given wait time");
    }
}
