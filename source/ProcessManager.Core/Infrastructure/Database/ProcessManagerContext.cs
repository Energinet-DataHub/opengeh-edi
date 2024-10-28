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

using Energinet.DataHub.ProcessManagement.Core.Domain.OrchestrationDescription;
using Energinet.DataHub.ProcessManagement.Core.Domain.OrchestrationInstance;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.ProcessManagement.Core.Infrastructure.Database;

public class ProcessManagerContext : DbContext
{
    public ProcessManagerContext(DbContextOptions<ProcessManagerContext> options)
        : base(options)
    {
    }

    public DbSet<OrchestrationDescription> OrchestrationDescriptions { get; private set; }

    public DbSet<OrchestrationInstance> OrchestrationInstances { get; private set; }

    public override int SaveChanges()
    {
        throw new NotSupportedException("Use the async version instead");
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        throw new NotSupportedException("Use the async version instead");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("pm");
        modelBuilder.ApplyConfiguration(new OrchestrationDescriptionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new OrchestrationInstanceEntityConfiguration());
    }
}
