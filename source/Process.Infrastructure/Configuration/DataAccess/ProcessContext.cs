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

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.DataAccess.Extensions.DbContext;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices;
using Energinet.DataHub.EDI.Process.Infrastructure.InboxEvents;
using Energinet.DataHub.EDI.Process.Infrastructure.InternalCommands;
using Energinet.DataHub.EDI.Process.Infrastructure.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Infrastructure.Transactions.WholesaleServices;
using Microsoft.EntityFrameworkCore;
using ExecutionContext = Energinet.DataHub.EDI.BuildingBlocks.Domain.ExecutionContext;

namespace Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess;

public class ProcessContext : DbContext
{
    private readonly ExecutionContext _executionContext;
    private readonly AuthenticatedActor _authenticatedActor;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;

#nullable disable
    public ProcessContext(
        DbContextOptions<ProcessContext> options,
        ExecutionContext executionContext,
        AuthenticatedActor authenticatedActor,
        ISystemDateTimeProvider systemDateTimeProvider)
        : base(options)
    {
        _executionContext = executionContext;
        _authenticatedActor = authenticatedActor;
        _systemDateTimeProvider = systemDateTimeProvider;
    }

    public ProcessContext()
    {
    }

    public DbSet<AggregatedMeasureDataProcess> AggregatedMeasureDataProcesses { get; private set; }

    public DbSet<QueuedInternalCommand> QueuedInternalCommands { get; private set; }

    public DbSet<ReceivedInboxEvent> ReceivedInboxEvents { get; private set; }

    public DbSet<WholesaleServicesProcess> WholesaleServicesProcesses { get; private set; }

    public override int SaveChanges()
    {
        throw new NotSupportedException("Use the async version instead");
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        throw new NotSupportedException("Use the async version instead");
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        this.UpdateAuditFields(_executionContext, _authenticatedActor, _systemDateTimeProvider);
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        this.UpdateAuditFields(_executionContext, _authenticatedActor, _systemDateTimeProvider);
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new AggregatedMeasureDataProcessEntityConfiguration());
        modelBuilder.ApplyConfiguration(new QueuedInternalCommandEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ReceivedInboxEventEntityConfiguration());
        modelBuilder.ApplyConfiguration(new WholesaleServicesProcessEntityConfiguration());
    }
}
