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
public class MonitorCalculationUsingClientsScenario : IAsyncLifetime
{
    public MonitorCalculationUsingClientsScenario(
        ProcessManagerClientFixture fixture,
        ITestOutputHelper testOutputHelper)
    {
        Fixture = fixture;
        Fixture.SetTestOutputHelper(testOutputHelper);

        var services = new ServiceCollection();
        services.AddScoped<IConfiguration>(_ => CreateInMemoryConfigurations(new Dictionary<string, string?>()
        {
            [$"{ProcessManagerHttpClientsOptions.SectionName}:{nameof(ProcessManagerHttpClientsOptions.GeneralApiBaseAddress)}"]
                = Fixture.ProcessManagerAppManager.AppHostManager.HttpClient.BaseAddress!.ToString(),
            [$"{ProcessManagerHttpClientsOptions.SectionName}:{nameof(ProcessManagerHttpClientsOptions.OrchestrationsApiBaseAddress)}"]
                = Fixture.OrchestrationsAppManager.AppHostManager.HttpClient.BaseAddress!.ToString(),
        }));
        services.AddProcessManagerHttpClients();
        ServiceProvider = services.BuildServiceProvider();
    }

    public ProcessManagerClientFixture Fixture { get; }

    private ServiceProvider ServiceProvider { get; }

    public Task InitializeAsync()
    {
        Fixture.ProcessManagerAppManager.AppHostManager.ClearHostLog();
        Fixture.OrchestrationsAppManager.AppHostManager.ClearHostLog();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        Fixture.SetTestOutputHelper(null);

        await ServiceProvider.DisposeAsync();
    }

    [Fact]
    public async Task CalculationBrs023_WhenScheduledToRunInThePastUsingClient_CanMonitorLifecycle()
    {
        var calculationClient = ServiceProvider.GetRequiredService<ClientTypes.Energinet.DataHub.ProcessManager.Client.Processes.BRS_023_027.V1.INotifyAggregatedMeasureDataClientV1>();

        // Step 1: Schedule new calculation orchestration instance
        var orchestrationInstanceId = await calculationClient
            .ScheduleNewCalculationAsync(
                new ClientTypes.Energinet.DataHub.ProcessManager.Api.Model.ScheduleOrchestrationInstanceCommand<NotifyAggregatedMeasureDataInputV1>(
                    operatingIdentity: new ClientTypes.Energinet.DataHub.ProcessManager.Api.Model.OrchestrationInstance.UserIdentityDto(
                        UserId: Guid.NewGuid(),
                        ActorId: Guid.NewGuid()),
                    runAt: DateTimeOffset.Parse("2024-11-01T06:19:10.0209567+01:00"),
                    inputParameter: new NotifyAggregatedMeasureDataInputV1(
                        CalculationTypes.BalanceFixing,
                        GridAreaCodes: new[] { "543" },
                        PeriodStartDate: DateTimeOffset.Parse("2024-10-29T15:19:10.0151351+01:00"),
                        PeriodEndDate: DateTimeOffset.Parse("2024-10-29T16:19:10.0193962+01:00"),
                        IsInternalCalculation: true)),
                CancellationToken.None);

        // Step 2: Trigger the scheduler to queue the calculation orchestration instance
        await Fixture.ProcessManagerAppManager.AppHostManager
            .TriggerFunctionAsync("StartScheduledOrchestrationInstances");

        // Step 3: Query until terminated with succeeded
        var isTerminated = await Awaiter.TryWaitUntilConditionAsync(
            async () =>
            {
                var orchestrationInstance = await calculationClient.GetCalculationAsync(orchestrationInstanceId, CancellationToken.None);

                return
                    orchestrationInstance.Lifecycle.State == ClientTypes.Energinet.DataHub.ProcessManager.Api.Model.OrchestrationInstance.OrchestrationInstanceLifecycleStates.Terminated
                    && orchestrationInstance.Lifecycle.TerminationState == ClientTypes.Energinet.DataHub.ProcessManager.Api.Model.OrchestrationInstance.OrchestrationInstanceTerminationStates.Succeeded;
            },
            timeLimit: TimeSpan.FromSeconds(60),
            delay: TimeSpan.FromSeconds(3));

        isTerminated.Should().BeTrue("because we expects the orchestration instance can complete within given wait time");
    }

    [Fact]
    public async Task CalculationBrs023ScheduledToRunInTheFuture_WhenCanceledUsingClient_CanMonitorLifecycle()
    {
        var generalClient = ServiceProvider.GetRequiredService<ClientTypes.Energinet.DataHub.ProcessManager.Client.IProcessManagerClient>();
        var calculationClient = ServiceProvider.GetRequiredService<ClientTypes.Energinet.DataHub.ProcessManager.Client.Processes.BRS_023_027.V1.INotifyAggregatedMeasureDataClientV1>();

        // Step 1: Schedule new calculation orchestration instance
        var orchestrationInstanceId = await calculationClient
            .ScheduleNewCalculationAsync(
                new ClientTypes.Energinet.DataHub.ProcessManager.Api.Model.ScheduleOrchestrationInstanceCommand<NotifyAggregatedMeasureDataInputV1>(
                    operatingIdentity: new ClientTypes.Energinet.DataHub.ProcessManager.Api.Model.OrchestrationInstance.UserIdentityDto(
                        UserId: Guid.NewGuid(),
                        ActorId: Guid.NewGuid()),
                    runAt: DateTimeOffset.Parse("2050-11-01T06:19:10.0209567+01:00"),
                    inputParameter: new NotifyAggregatedMeasureDataInputV1(
                        CalculationTypes.BalanceFixing,
                        GridAreaCodes: new[] { "543" },
                        PeriodStartDate: DateTimeOffset.Parse("2024-10-29T15:19:10.0151351+01:00"),
                        PeriodEndDate: DateTimeOffset.Parse("2024-10-29T16:19:10.0193962+01:00"),
                        IsInternalCalculation: true)),
                CancellationToken.None);

        // Step 2: Cancel the calculation orchestration instance
        await generalClient.CancelScheduledOrchestrationInstanceAsync(
            new ClientTypes.Energinet.DataHub.ProcessManager.Api.Model.CancelScheduledOrchestrationInstanceCommand(
                operatingIdentity: new ClientTypes.Energinet.DataHub.ProcessManager.Api.Model.OrchestrationInstance.UserIdentityDto(
                    UserId: Guid.NewGuid(),
                    ActorId: Guid.NewGuid()),
                orchestrationInstanceId),
            CancellationToken.None);

        // Step 3: Query until terminated with canceled
        var isTerminated = await Awaiter.TryWaitUntilConditionAsync(
            async () =>
            {
                var orchestrationInstance = await calculationClient.GetCalculationAsync(orchestrationInstanceId, CancellationToken.None);

                return
                    orchestrationInstance.Lifecycle.State == ClientTypes.Energinet.DataHub.ProcessManager.Api.Model.OrchestrationInstance.OrchestrationInstanceLifecycleStates.Terminated
                    && orchestrationInstance.Lifecycle.TerminationState == ClientTypes.Energinet.DataHub.ProcessManager.Api.Model.OrchestrationInstance.OrchestrationInstanceTerminationStates.UserCanceled;
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
