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

// IMPORTANT:
// Since we use shared types (linked files) and the test project needs a reference
// to multiple projects where files are linked, we need to specify which assembly
// we want to use the type from.
// See also https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs0433?f1url=%3FappId%3Droslyn%26k%3Dk(CS0433)
extern alias ClientTypes;

using ClientTypes.Energinet.DataHub.ProcessManager.Client.Extensions.DependencyInjection;
using ClientTypes.Energinet.DataHub.ProcessManager.Client.Extensions.Options;
using ClientTypes.Energinet.DataHub.ProcessManager.Orchestrations.Processes.BRS_023_027.V1.Model;
using Energinet.DataHub.Core.FunctionApp.TestCommon.FunctionAppHost;
using Energinet.DataHub.Core.TestCommon;
using Energinet.DataHub.ProcessManager.Client.Tests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Energinet.DataHub.ProcessManager.Client.Tests.Integration;

/// <summary>
/// Test case where we verify the Process Manager clients can be used to start a
/// calculation orchestration and monitor its status during its lifetime.
/// </summary>
[Collection(nameof(ProcessManagerClientCollection))]
public class MonitorCalculationScenarioUsingClients : IAsyncLifetime
{
    public MonitorCalculationScenarioUsingClients(
        ScenarioProcessManagerAppFixture processManagerAppFixture,
        ScenarioOrchestrationsAppFixture orchestrationsAppFixture,
        ITestOutputHelper testOutputHelper)
    {
        ProcessManagerAppFixture = processManagerAppFixture;
        ProcessManagerAppFixture.SetTestOutputHelper(testOutputHelper);

        OrchestrationsAppFixture = orchestrationsAppFixture;
        OrchestrationsAppFixture.SetTestOutputHelper(testOutputHelper);

        var services = new ServiceCollection();
        services.AddScoped<IConfiguration>(_ => CreateInMemoryConfigurations(new Dictionary<string, string?>()
        {
            [$"{ProcessManagerClientOptions.SectionName}:{nameof(ProcessManagerClientOptions.GeneralApiBaseAddress)}"]
                = ProcessManagerAppFixture.AppHostManager.HttpClient.BaseAddress!.ToString(),
            [$"{ProcessManagerClientOptions.SectionName}:{nameof(ProcessManagerClientOptions.OrchestrationsApiBaseAddress)}"]
                = OrchestrationsAppFixture.AppHostManager.HttpClient.BaseAddress!.ToString(),
        }));
        services.AddProcessManagerClients();
        ServiceProvider = services.BuildServiceProvider();
    }

    private ScenarioProcessManagerAppFixture ProcessManagerAppFixture { get; }

    private ScenarioOrchestrationsAppFixture OrchestrationsAppFixture { get; }

    private ServiceProvider ServiceProvider { get; }

    public Task InitializeAsync()
    {
        ProcessManagerAppFixture.AppHostManager.ClearHostLog();
        OrchestrationsAppFixture.AppHostManager.ClearHostLog();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        ProcessManagerAppFixture.SetTestOutputHelper(null!);
        OrchestrationsAppFixture.SetTestOutputHelper(null!);

        await ServiceProvider.DisposeAsync();
    }

    /// <summary>
    /// TODO: Change when we don't share types.
    /// At the moment we have no project references to the applications that we start.
    /// It means they are not automatically builded when changed, so we must ensure to build them
    /// before running tests.
    /// </summary>
    [Fact]
    public async Task CalculationBrs023_WhenScheduledUsingClient_CanMonitorLifecycle()
    {
        var calculationClient = ServiceProvider.GetRequiredService<ClientTypes.Energinet.DataHub.ProcessManager.Client.Processes.BRS_023_027.V1.INotifyAggregatedMeasureDataClientV1>();

        // Step 1: Schedule new calculation orchestration instance
        var orchestrationInstanceId = await calculationClient
            .ScheduleNewCalculationOrchestationInstanceAsync(
                new ClientTypes.Energinet.DataHub.ProcessManager.Api.Model.ScheduleOrchestrationInstanceDto<NotifyAggregatedMeasureDataInputV1>(
                    RunAt: DateTimeOffset.Parse("2024-11-01T06:19:10.0209567+01:00"),
                    InputParameter: new NotifyAggregatedMeasureDataInputV1(
                        CalculationTypes.BalanceFixing,
                        GridAreaCodes: new[] { "543" },
                        PeriodStartDate: DateTimeOffset.Parse("2024-10-29T15:19:10.0151351+01:00"),
                        PeriodEndDate: DateTimeOffset.Parse("2024-10-29T16:19:10.0193962+01:00"),
                        IsInternalCalculation: true)),
                CancellationToken.None);

        // Step 2: Trigger the scheduler to queue the calculation orchestration instance
        await ProcessManagerAppFixture.AppHostManager
            .TriggerFunctionAsync("StartScheduledOrchestrationInstances");

        // Step 3: Query until terminated with succeeded
        var isTerminated = await Awaiter.TryWaitUntilConditionAsync(
            async () =>
            {
                var orchestrationInstance = await calculationClient.GetCalculationOrchestrationInstanceAsync(orchestrationInstanceId, CancellationToken.None);

                return orchestrationInstance!.Lifecycle!.State == ClientTypes.Energinet.DataHub.ProcessManager.Api.Model.OrchestrationInstance.OrchestrationInstanceLifecycleStates.Terminated;
            },
            timeLimit: TimeSpan.FromSeconds(60),
            delay: TimeSpan.FromSeconds(3));

        isTerminated.Should().BeTrue("because we expects the orchestration instance can complete within given wait time");
    }

    private IConfiguration CreateInMemoryConfigurations(Dictionary<string, string?> configurations)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(configurations)
            .Build();
    }
}
