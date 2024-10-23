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
using Energinet.DataHub.ProcessManagement.Core.Infrastructure.OrchestrationsRegistration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Energinet.DataHub.ProcessManagement.Core.Infrastructure.Extensions.Startup;

/// <summary>
/// Provides extension methods for the <see cref="IHost"/> to ProcessManager related operations
/// we want to perform during startup.
/// </summary>
public static class HostExtensions
{
    /// <summary>
    /// Register and deregister orchestrations during application startup.
    /// </summary>
    public static async Task SynchronizeWithOrchestrationRegisterAsync(this IHost host)
    {
        var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(nameof(SynchronizeWithOrchestrationRegisterAsync));

        try
        {
            var enabledDescriptions = host.Services.GetRequiredService<IReadOnlyCollection<DFOrchestrationDescription>>();
            var synchronizer = host.Services.GetRequiredService<OrchestrationRegisterSynchronizer>();
            await synchronizer
                .SynchronizeAsync(
                    hostName: "ProcessManager.Orchestrations",
                    enabledDescriptions)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not register orchestrations during startup.");
        }
    }
}
