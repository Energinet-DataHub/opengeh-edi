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

/// <summary>
/// Keep a register of known Durable Functions orchestrations.
/// Each orchestration is registered with information by which
/// it is possible to communicate with Durable Functions and
/// start a new orchestration instance.
/// </summary>
public class OrchestrationRegister
{
    private readonly List<DFOrchestrationDescription> _knownOrchestrationDescriptions = [];

    public Task<IReadOnlyCollection<DFOrchestrationDescription>> GetAllByHostNameAsync(string hostName)
    {
        return Task.FromResult((IReadOnlyCollection<DFOrchestrationDescription>)_knownOrchestrationDescriptions
            .Where(x =>
                x.HostName == hostName)
            .ToList());
    }

    public Task<DFOrchestrationDescription?> GetOrDefaultAsync(string name, int version, bool isEnabled = true)
    {
        return Task.FromResult(_knownOrchestrationDescriptions
            .SingleOrDefault(x =>
                x.Name == name
                && x.Version == version
                && x.IsEnabled == isEnabled));
    }

    /// <summary>
    /// Durable Functions orchestration host's can use this method to register the orchestrations
    /// they host.
    /// </summary>
    /// <param name="orchestrationDescription"></param>
    /// <exception cref="InvalidOperationException">Thrown if an orchestration description with the
    /// same version and name has been registered before.</exception>
    public Task RegisterAsync(DFOrchestrationDescription orchestrationDescription)
    {
        if (_knownOrchestrationDescriptions
            .Any(x =>
                x.Name == orchestrationDescription.Name
                && x.Version == orchestrationDescription.Version))
        {
            throw new InvalidOperationException("Orchestration description has been registered before.");
        }

        _knownOrchestrationDescriptions.Add(orchestrationDescription);

        return Task.CompletedTask;
    }

    public Task DeregisterAsync(string name, int version)
    {
        var orchestratorDescription = _knownOrchestrationDescriptions
            .SingleOrDefault(x =>
                x.Name == name
                && x.Version == version);

        if (orchestratorDescription == null)
        {
            throw new InvalidOperationException("Orchestration description has not been registered.");
        }

        orchestratorDescription.IsEnabled = false;

        return Task.CompletedTask;
    }
}
