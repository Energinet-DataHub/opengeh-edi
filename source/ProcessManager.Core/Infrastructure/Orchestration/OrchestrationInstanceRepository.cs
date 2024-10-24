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

namespace Energinet.DataHub.ProcessManagement.Core.Infrastructure.Orchestration;

public class OrchestrationInstanceRepository : IOrchestrationInstanceRepository
{
    private readonly ProcessManagerContext _context;

    public OrchestrationInstanceRepository(ProcessManagerContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task AddAsync(OrchestrationInstance orchestrationInstance)
    {
        await _context.OrchestrationInstances.AddAsync(orchestrationInstance).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<OrchestrationInstance> GetAsync(OrchestrationInstanceId id)
    {
        return await _context.OrchestrationInstances.FirstAsync(x => x.Id == id).ConfigureAwait(false);
    }
}
