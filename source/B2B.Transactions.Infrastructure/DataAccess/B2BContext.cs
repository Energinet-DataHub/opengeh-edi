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
using B2B.Transactions.Infrastructure.Configuration.InternalCommands;
using B2B.Transactions.Infrastructure.DataAccess.Outgoing;
using B2B.Transactions.Infrastructure.DataAccess.Transaction;
using B2B.Transactions.OutgoingMessages;
using B2B.Transactions.Transactions;
using Microsoft.EntityFrameworkCore;

namespace B2B.Transactions.Infrastructure.DataAccess
{
    public class B2BContext : DbContext
    {
        #nullable disable
        public B2BContext(DbContextOptions<B2BContext> options)
            : base(options)
        {
        }

        public B2BContext()
        {
        }

        public DbSet<AcceptedTransaction> Transactions { get; private set; }

        public DbSet<OutgoingMessage> OutgoingMessages { get; private set; }

        public DbSet<QueuedInternalCommand> QueuedInternalCommands { get; private set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (modelBuilder == null) throw new ArgumentNullException(nameof(modelBuilder));

            modelBuilder.ApplyConfiguration(new TransactionEntityConfiguration());
            modelBuilder.ApplyConfiguration(new OutgoingMessageEntityConfiguration());
            modelBuilder.ApplyConfiguration(new QueuedInternalCommandEntityConfiguration());
        }
    }
}
