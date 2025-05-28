﻿// Copyright 2020 Energinet DataHub A/S
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

using System.Data;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.DataAccess.Extensions.DbContext;
using Energinet.DataHub.EDI.OutgoingMessages.Application;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Repositories.ActorMessageQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Repositories.Bundles;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Repositories.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Repositories.OutgoingMessages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.DataAccess;

public class ActorMessageQueueContext : DbContext, IEdiDbContext, IActorMessageQueueContext
{
    private readonly BuildingBlocks.Domain.ExecutionContext _executionContext;
    private readonly AuthenticatedActor _authenticatedActor;
    private readonly IClock _clock;

#nullable disable
    public ActorMessageQueueContext(
        DbContextOptions<ActorMessageQueueContext> options,
        BuildingBlocks.Domain.ExecutionContext executionContext,
        AuthenticatedActor authenticatedActor,
        IClock clock)
        : base(options)
    {
        _executionContext = executionContext;
        _authenticatedActor = authenticatedActor;
        _clock = clock;
    }

    /// <summary>
    /// Used to supports tests
    /// </summary>
    public ActorMessageQueueContext(DbContextOptions<ActorMessageQueueContext> options)
        : base(options)
    {
        _executionContext = new BuildingBlocks.Domain.ExecutionContext();
        _executionContext.SetExecutionType(ExecutionType.Test);
        _authenticatedActor = new AuthenticatedActor();
        _clock = SystemClock.Instance;
    }

    public DbSet<OutgoingMessage> OutgoingMessages { get; private set; }

    public DbSet<ActorMessageQueue> ActorMessageQueues { get; private set; }

    public DbSet<MarketDocument> MarketDocuments { get; private set; }

    public DbSet<Bundle> Bundles { get; private set; }

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
        this.UpdateAuditFields(_executionContext, _authenticatedActor, _clock);
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        this.UpdateAuditFields(_executionContext, _authenticatedActor, _clock);
        return base.SaveChangesAsync(cancellationToken);
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

    public Task CommitTransactionAsync(
        IDbContextTransaction transaction,
        CancellationToken cancellationToken = default) => transaction.CommitAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new OutgoingMessageEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ActorMessageQueueEntityConfiguration());
        modelBuilder.ApplyConfiguration(new BundleEntityConfiguration());
        modelBuilder.ApplyConfiguration(new MarketDocumentEntityConfiguration());
    }
}
