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
using Energinet.DataHub.EDI.Domain.ArchivedMessages;
using Energinet.DataHub.EDI.Infrastructure.ArchivedMessages;
using Energinet.DataHub.EDI.Infrastructure.Configuration.Serialization;
using Energinet.DataHub.EDI.Infrastructure.InboxEvents;
using Energinet.DataHub.EDI.Infrastructure.IncomingMessages;
using Energinet.DataHub.EDI.Process.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.Process.Domain.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Process.Infrastructure.InternalCommands;
using Energinet.DataHub.EDI.Process.Infrastructure.OutgoingMessages;
using Energinet.DataHub.EDI.Process.Infrastructure.OutgoingMessages.Queueing;
using Energinet.DataHub.EDI.Process.Infrastructure.Transactions.AggregatedMeasureData;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.EDI.Process.Infrastructure.Configuration.DataAccess
{
    public class ProcessContext : DbContext
    {
        private readonly ISerializer _serializer;

        #nullable disable
        public ProcessContext(DbContextOptions<ProcessContext> options, ISerializer serializer)
            : base(options)
        {
            _serializer = serializer;
        }

        public ProcessContext()
        {
        }

        public DbSet<AggregatedMeasureDataProcess> AggregatedMeasureDataProcesses { get; private set; }

        public DbSet<OutgoingMessage> OutgoingMessages { get; private set; }

        public DbSet<QueuedInternalCommand> QueuedInternalCommands { get; private set; }

        public DbSet<EnqueuedMessage> EnqueuedMessages { get; private set; }

        public DbSet<ActorMessageQueue> ActorMessageQueues { get; private set; }

        public DbSet<MarketDocument> MarketDocuments { get; private set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (modelBuilder == null) throw new ArgumentNullException(nameof(modelBuilder));

            modelBuilder.ApplyConfiguration(new AggregatedMeasureDataProcessEntityConfiguration());
            modelBuilder.ApplyConfiguration(new OutgoingMessageEntityConfiguration());
            modelBuilder.ApplyConfiguration(new QueuedInternalCommandEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ArchivedMessageEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ActorMessageQueueEntityConfiguration());
            modelBuilder.ApplyConfiguration(new MarketDocumentEntityConfiguration());
        }
    }
}
