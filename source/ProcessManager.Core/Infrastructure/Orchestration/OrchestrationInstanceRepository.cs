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

using Energinet.DataHub.ProcessManagement.Core.Application.Orchestration;
using Energinet.DataHub.ProcessManagement.Core.Application.Scheduling;
using Energinet.DataHub.ProcessManagement.Core.Domain.OrchestrationInstance;
using Energinet.DataHub.ProcessManagement.Core.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace Energinet.DataHub.ProcessManagement.Core.Infrastructure.Orchestration;

/// <summary>
/// Read/write access to the orchestration instance repository.
/// </summary>
internal class OrchestrationInstanceRepository(
    ProcessManagerContext context) :
        IOrchestrationInstanceRepository,
        IOrchestrationInstanceProgressRepository,
        IOrchestrationInstanceQueries,
        IScheduledOrchestrationInstancesByInstantQuery
{
    private readonly ProcessManagerContext _context = context;

    /// <inheritdoc />
    public IUnitOfWork UnitOfWork => _context;

    /// <inheritdoc />
    public Task<OrchestrationInstance> GetAsync(OrchestrationInstanceId id)
    {
        ArgumentNullException.ThrowIfNull(id);

        return _context.OrchestrationInstances.FirstAsync(x => x.Id == id);
    }

    /// <inheritdoc />
    public async Task AddAsync(OrchestrationInstance orchestrationInstance)
    {
        ArgumentNullException.ThrowIfNull(orchestrationInstance);

        await _context.OrchestrationInstances.AddAsync(orchestrationInstance).ConfigureAwait(false);
    }

    /// <inheritdoc cref="IScheduledOrchestrationInstancesByInstantQuery.FindAsync(Instant)"/>
    public async Task<IReadOnlyCollection<OrchestrationInstance>> FindAsync(Instant scheduledToRunBefore)
    {
        var query = _context.OrchestrationInstances
            .Where(x => x.Lifecycle.State == OrchestrationInstanceLifecycleStates.Pending)
            .Where(x => x.Lifecycle.ScheduledToRunAt != null && x.Lifecycle.ScheduledToRunAt.Value <= scheduledToRunBefore);

        return await query.ToListAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<OrchestrationInstance>> SearchAsync(
        string name,
        int? version = default,
        OrchestrationInstanceLifecycleStates? lifecycleState = default,
        OrchestrationInstanceTerminationStates? terminationState = default,
        Instant? startedAtOrLater = default,
        Instant? terminatedAtOrEarlier = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var query = _context
            .OrchestrationDescriptions
                .Where(x => x.Name == name)
                .Where(x => version == null || x.Version == version)
            .Join(
                _context.OrchestrationInstances,
                description => description.Id,
                instance => instance.OrchestrationDescriptionId,
                (_, instance) => instance)
            .Where(x => lifecycleState == null || x.Lifecycle.State == lifecycleState)
            .Where(x => terminationState == null || x.Lifecycle.TerminationState == terminationState)
            .Where(x => startedAtOrLater == null || x.Lifecycle.StartedAt >= startedAtOrLater)
            .Where(x => terminatedAtOrEarlier == null || x.Lifecycle.TerminatedAt <= terminatedAtOrEarlier);

        return await query.ToListAsync().ConfigureAwait(false);
    }
}
