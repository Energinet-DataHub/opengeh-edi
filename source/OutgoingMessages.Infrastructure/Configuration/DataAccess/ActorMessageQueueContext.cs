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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Energinet.DataHub.EDI.DataAccess.Extensions.DbContext;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.OutgoingMessages.Queueing;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Configuration.DataAccess
{
    public class ActorMessageQueueContext : DbContext
    {
        private readonly Energinet.DataHub.EDI.BuildingBlocks.Domain.ExecutionContext _executionContext;
        private readonly AuthenticatedActor _authenticatedActor;
        private readonly ISystemDateTimeProvider _systemDateTimeProvider;

#nullable disable
        public ActorMessageQueueContext(
            DbContextOptions<ActorMessageQueueContext> options,
            Energinet.DataHub.EDI.BuildingBlocks.Domain.ExecutionContext executionContext,
            AuthenticatedActor authenticatedActor,
            ISystemDateTimeProvider systemDateTimeProvider)
            : base(options)
        {
            _executionContext = executionContext;
            _authenticatedActor = authenticatedActor;
            _systemDateTimeProvider = systemDateTimeProvider;
        }

        public DbSet<OutgoingMessage> OutgoingMessages { get; private set; }

        public DbSet<ActorMessageQueue> ActorMessageQueues { get; private set; }

        public DbSet<MarketDocument> MarketDocuments { get; private set; }

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

            modelBuilder.ApplyConfiguration(new OutgoingMessageEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ActorMessageQueueEntityConfiguration());
            modelBuilder.ApplyConfiguration(new MarketDocumentEntityConfiguration());
        }
    }
}
