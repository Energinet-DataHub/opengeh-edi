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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.OutgoingMessages.Queueing;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Configuration.DataAccess
{
    public class ActorMessageQueueContext : DbContext
    {
        #nullable disable
        public ActorMessageQueueContext(DbContextOptions<ActorMessageQueueContext> options)
            : base(options)
        {
        }

        public ActorMessageQueueContext()
        {
        }

        public DbSet<OutgoingMessage> OutgoingMessages { get; private set; }

        public DbSet<ActorMessageQueue> ActorMessageQueues { get; private set; }

        public DbSet<MarketDocument> MarketDocuments { get; private set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ArgumentNullException.ThrowIfNull(modelBuilder);

            modelBuilder.ApplyConfiguration(new OutgoingMessageEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ActorMessageQueueEntityConfiguration());
            modelBuilder.ApplyConfiguration(new MarketDocumentEntityConfiguration());
        }
    }
}
