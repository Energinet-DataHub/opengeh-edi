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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DateTime;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.DataAccess.DataAccess;
using Energinet.DataHub.EDI.DataAccess.Extensions.DbContext;
using Energinet.DataHub.EDI.Outbox.Domain;
using Microsoft.EntityFrameworkCore;
using ExecutionContext = Energinet.DataHub.EDI.BuildingBlocks.Domain.ExecutionContext;

namespace Energinet.DataHub.EDI.Outbox.Infrastructure;

public class OutboxContext : DbContext, IEdiDbContext
{
    private readonly ExecutionContext _executionContext;
    private readonly AuthenticatedActor _authenticatedActor;
    private readonly ISystemDateTimeProvider _systemDateTimeProvider;

    public OutboxContext(
        DbContextOptions<OutboxContext> options,
        ExecutionContext executionContext,
        AuthenticatedActor authenticatedActor,
        ISystemDateTimeProvider systemDateTimeProvider)
        : base(options)
    {
        _executionContext = executionContext;
        _authenticatedActor = authenticatedActor;
        _systemDateTimeProvider = systemDateTimeProvider;
    }

    /// <summary>
    /// Used to supports tests
    /// </summary>
    public OutboxContext(DbContextOptions<OutboxContext> options)
        : base(options)
    {
        _executionContext = new ExecutionContext();
        _executionContext.SetExecutionType(ExecutionType.Test);
        _authenticatedActor = new AuthenticatedActor();
        _systemDateTimeProvider = new SystemDateTimeProvider();
    }

    // ReSharper disable once UnusedAutoPropertyAccessor.Local -- Used by EF
    public DbSet<OutboxMessage> Outbox { get; private set; }

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
        modelBuilder.ApplyConfiguration(new OutboxEntityConfiguration());
    }
}
