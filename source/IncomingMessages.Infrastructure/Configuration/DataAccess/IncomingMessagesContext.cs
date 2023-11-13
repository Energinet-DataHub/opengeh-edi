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

using Microsoft.EntityFrameworkCore;

namespace IncomingMessages.Infrastructure.Configuration.DataAccess
{
    public class IncomingMessagesContext : DbContext
    {
        #nullable disable
        public IncomingMessagesContext(DbContextOptions<IncomingMessagesContext> options)
            : base(options)
        {
        }

        public IncomingMessagesContext()
        {
        }

        public DbSet<MessageIdForSender> MessageIdForSenders { get; private set; }

        public DbSet<TransactionIdForSender> TransactionIdForSenders { get; private set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (modelBuilder == null) throw new ArgumentNullException(nameof(modelBuilder));

            modelBuilder.ApplyConfiguration(new MessageIdEntityConfiguration());
            modelBuilder.ApplyConfiguration(new TransactionIdEntityConfiguration());
        }
    }
}
