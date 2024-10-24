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

using Energinet.DataHub.ProcessManagement.Core.Application;
using Energinet.DataHub.ProcessManagement.Core.Domain;
using Energinet.DataHub.ProcessManagement.Core.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.ProcessManagement.Core.Infrastructure.OrchestrationsRegistration;

/// <summary>
/// Keep a register of known Durable Functions orchestrations.
/// Each orchestration is registered with information by which it is possible
/// to communicate with Durable Functions and start a new orchestration instance.
/// </summary>
public class OrchestrationRegister : IOrchestrationRegister, IOrchestrationRegisterQueries
{
    private readonly ProcessManagerContext _context;

    public OrchestrationRegister(ProcessManagerContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public Task<OrchestrationDescription?> GetOrDefaultAsync(string name, int version, bool isEnabled = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return _context.OrchestrationDescriptions
            .SingleOrDefaultAsync(x =>
                x.Name == name
                && x.Version == version
                && x.IsEnabled == isEnabled);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<OrchestrationDescription>> GetAllByHostNameAsync(string hostName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostName);

        var query = _context.OrchestrationDescriptions
            .Where(x
                => x.HostName == hostName);

        return await query.ToListAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RegisterAsync(OrchestrationDescription orchestrationDescription)
    {
        ArgumentNullException.ThrowIfNull(orchestrationDescription);

        var existing = await GetOrDefaultAsync(orchestrationDescription.Name, orchestrationDescription.Version).ConfigureAwait(false);
        if (existing == null)
        {
            _context.Add(orchestrationDescription);
        }
        else
        {
            existing.IsEnabled = true;
        }

        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeregisterAsync(OrchestrationDescription orchestrationDescription)
    {
        ArgumentNullException.ThrowIfNull(orchestrationDescription);

        var existing = await GetOrDefaultAsync(orchestrationDescription.Name, orchestrationDescription.Version).ConfigureAwait(false);

        if (existing == null)
            throw new InvalidOperationException("Orchestration description has not been registered.");

        existing.IsEnabled = false;

        await _context.SaveChangesAsync().ConfigureAwait(false);
    }
}
