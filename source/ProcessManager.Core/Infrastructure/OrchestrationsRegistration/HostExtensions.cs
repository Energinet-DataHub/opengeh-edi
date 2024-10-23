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

using Energinet.DataHub.ProcessManagement.Core.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.ProcessManagement.Core.Infrastructure.OrchestrationsRegistration;

/// <summary>
/// Provides extension methods for the <see cref="IHost"/> to ProcessManager related operations
/// we want to perform during startup.
/// </summary>
public static class HostExtensions
{
    /// <summary>
    /// Register and deregister orchestrations during startup.
    /// </summary>
    public static async Task SynchronizeOrchestrationsAsync(this IHost host)
    {
        var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(nameof(SynchronizeOrchestrationsAsync));

        try
        {
            var orchestrationDescriptions = BuildOrchestrationDescriptions();

            var registrator = host.Services.GetRequiredService<HostStartupRegistrator>();
            await registrator
                .SynchronizeHostOrchestrationsAsync(
                    hostName: "ProcessManager.Orchestrations",
                    orchestrationDescriptions)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not register orchestrations during startup.");
        }
    }

    /// <summary>
    /// Build descriptions for all Durable Function orchestrations that should be enabled.
    /// Leave out descriptions for any Durable Function orchestrations that should be disabled.
    /// </summary>
    private static IReadOnlyCollection<DFOrchestrationDescription> BuildOrchestrationDescriptions()
    {
        var brs_023_027_v1 = new DFOrchestrationDescription(
            name: "BRS_023_027",
            version: 1,
            canBeScheduled: true,
            functionName: "NotifyAggregatedMeasureDataOrchestrationV1");

        return
            [
                brs_023_027_v1
            ];
    }
}
