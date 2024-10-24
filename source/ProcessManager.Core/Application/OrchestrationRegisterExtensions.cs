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

namespace Energinet.DataHub.ProcessManagement.Core.Application;

public static class OrchestrationRegisterExtensions
{
    /// <summary>
    /// Synchronize the orchestration register with the Durable Functions orchestrations for an application host.
    /// Register any orchestration descriptions that doesn't already exists in the orchestration register.
    /// Disable any orchestration descriptions that doesn't exists in the application host.
    /// </summary>
    /// <param name="register">Orchestration register.</param>
    /// <param name="hostName">Name of the application hosting the Durable Functions orchestrations.</param>
    /// <param name="hostDescriptions">List of orchestration descriptions that describes Durable Function orchestrations
    /// known to the application host.</param>
    public static async Task SynchronizeAsync(
        this IOrchestrationRegister register,
        string hostName,
        IReadOnlyCollection<OrchestrationDescription> hostDescriptions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostName);
        ArgumentNullException.ThrowIfNull(hostDescriptions);

        var registerDescriptions = await register.GetAllByHostNameAsync(hostName).ConfigureAwait(false);

        // Deregister orchestrations not known to the host anymore
        foreach (var registerDescription in registerDescriptions)
        {
            var hostDescription = hostDescriptions
                .SingleOrDefault(x =>
                    x.Name == registerDescription.Name
                    && x.Version == registerDescription.Version);

            if (hostDescription == null)
                await register.DeregisterAsync(registerDescription.Name, registerDescription.Version).ConfigureAwait(false);
        }

        // Register orchestrations not known (or previously disabled) in the register
        foreach (var hostDescription in hostDescriptions)
        {
            var registerDescription = registerDescriptions
                .SingleOrDefault(x =>
                    x.Name == hostDescription.Name
                    && x.Version == hostDescription.Version);

            if (registerDescription == null || registerDescription.IsEnabled == false)
            {
                // Enforce certain values
                hostDescription.HostName = hostName;
                hostDescription.IsEnabled = true;

                await register.RegisterAsync(hostDescription).ConfigureAwait(false);
            }
        }
    }
}
