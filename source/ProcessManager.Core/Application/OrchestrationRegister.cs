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
    private readonly List<OrchestrationDescription> _knownOrchestrators = [];

    public OrchestrationDescription? GetOrchestratorDescriptionOrDefault(string name, int version)
    {
        return _knownOrchestrators
            .SingleOrDefault(x =>
                x.Name == name
                && x.Version == version);
    }

    public void Register(OrchestrationDescription orchestrator)
    {
        if (_knownOrchestrators
            .Any(x =>
                x.Name == orchestrator.Name
                && x.Version == orchestrator.Version))
        {
            throw new InvalidOperationException("Orchestrator has been registered before.");
        }

        _knownOrchestrators.Add(orchestrator);
    }

    public void Deregister(string name, int version)
    {
        var orchestratorDescription = _knownOrchestrators
            .SingleOrDefault(x =>
                x.Name == name
                && x.Version == version);

        if (orchestratorDescription == null)
        {
            throw new InvalidOperationException("Orchestrator has not been registered.");
        }

        _knownOrchestrators.Remove(orchestratorDescription);
    }
}
