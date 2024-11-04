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
using Energinet.DataHub.Core.App.Common.Extensions.DependencyInjection;
using Energinet.DataHub.Core.App.FunctionApp.Extensions.Builder;
using Energinet.DataHub.Core.App.FunctionApp.Extensions.DependencyInjection;
using Energinet.DataHub.ProcessManagement.Core.Application;
using Energinet.DataHub.ProcessManagement.Core.Infrastructure.Extensions.DependencyInjection;
using Energinet.DataHub.ProcessManagement.Core.Infrastructure.Telemetry;
using Energinet.DataHub.ProcessManager.Orchestrations.Processes.BRS_023_027.V1.Model;
using Energinet.DataHub.ProcessManager.Scheduler;
using Microsoft.EntityFrameworkCore.SqlServer.NodaTime.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NodaTime;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        // Common
        services.AddApplicationInsightsForIsolatedWorker(TelemetryConstants.SubsystemName);
        services.AddHealthChecksForIsolatedWorker();
        services.AddNodaTimeForApplication();

        // Scheduler
        services.AddScoped<SchedulerHandler>();

        // ProcessManager
        services.AddProcessManagerCore();
    })
    .ConfigureLogging((hostingContext, logging) =>
    {
        logging.AddLoggingConfigurationForIsolatedWorker(hostingContext);
    })
    .Build();

// TODO: For demo purposes; remove when done
var runDemo = false;
if (runDemo)
{
    // Wait to allow orchestartion description to be registered.
    // This is only necessary if the database is empty and hence the orchestration description was not registered previously.
    await Task.Delay(TimeSpan.FromSeconds(30)).ConfigureAwait(false);

    // Must match the parameter definition registered, with regards to properties
    dynamic parameter = new ExpandoObject();
    parameter.CalculationType = CalculationTypes.BalanceFixing;
    parameter.GridAreaCodes = new[] { "543" };
    parameter.StartDate = DateTimeOffset.Now;
    parameter.EndDate = DateTimeOffset.Now.AddHours(1);
    parameter.ScheduledAt = DateTimeOffset.Now;
    parameter.IsInternalCalculation = true;

    var manager = host.Services.GetRequiredService<IOrchestrationInstanceManager>();
    var clock = host.Services.GetRequiredService<IClock>();
    await manager.ScheduleNewOrchestrationInstanceAsync(
        name: "BRS_023_027",
        version: 1,
        parameter: parameter,
        runAt: clock.GetCurrentInstant().PlusSeconds(20))
        .ConfigureAwait(false);
}

host.Run();
