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
using Domain.ArchivedMessages;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.AccountingPointCharacteristics;
using Domain.OutgoingMessages.CharacteristicsOfACustomerAtAnAp;
using Domain.OutgoingMessages.ConfirmRequestChangeAccountingPointCharacteristics;
using Domain.OutgoingMessages.ConfirmRequestChangeOfSupplier;
using Domain.OutgoingMessages.GenericNotification;
using Domain.OutgoingMessages.Peek;
using Domain.OutgoingMessages.RejectRequestChangeAccountingPointCharacteristics;
using Domain.OutgoingMessages.RejectRequestChangeOfSupplier;
using Domain.Transactions.AggregatedMeasureData;
using Domain.Transactions.Aggregations;
using Domain.Transactions.MoveIn;
using Infrastructure.ArchivedMessages;
using Infrastructure.Configuration.IntegrationEvents;
using Infrastructure.Configuration.InternalCommands;
using Infrastructure.Configuration.Serialization;
using Infrastructure.MasterData.MarketEvaluationPoints;
using Infrastructure.OutgoingMessages;
using Infrastructure.OutgoingMessages.Peek;
using Infrastructure.Transactions;
using Infrastructure.Transactions.AggregatedMeasureData;
using Infrastructure.Transactions.Aggregations;
using Infrastructure.Transactions.UpdateCustomer;
using Microsoft.EntityFrameworkCore;
using MarketEvaluationPoint = Domain.MasterData.MarketEvaluationPoints.MarketEvaluationPoint;

namespace Infrastructure.Configuration.DataAccess
{
    public class B2BContext : DbContext
    {
        private readonly ISerializer _serializer;

        #nullable disable
        public B2BContext(DbContextOptions<B2BContext> options, ISerializer serializer)
            : base(options)
        {
            _serializer = serializer;
        }

        public B2BContext()
        {
        }

        public DbSet<MoveInTransaction> Transactions { get; private set; }

        public DbSet<AggregatedMeasureDataProcess> AggregatedMeasureDataProcesses { get; private set; }

        public DbSet<AggregationResultForwarding> AggregatedTimeSeriesTransactions { get; private set; }

        public DbSet<OutgoingMessage> OutgoingMessages { get; private set; }

        public DbSet<QueuedInternalCommand> QueuedInternalCommands { get; private set; }

        public DbSet<MarketEvaluationPoint> MarketEvaluationPoints { get; private set; }

        public DbSet<EnqueuedMessage> EnqueuedMessages { get; private set; }

        public DbSet<BundledMessage> BundledMessages { get; private set; }

        public DbSet<ArchivedMessage> ArchivedMessages { get; private set; }

        public DbSet<ReceivedIntegrationEvent> ReceivedIntegrationEvents { get; private set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (modelBuilder == null) throw new ArgumentNullException(nameof(modelBuilder));

            modelBuilder.ApplyConfiguration(new MoveInTransactionEntityConfiguration(_serializer));
            modelBuilder.ApplyConfiguration(new AggregatedMeasureDataProcessEntityConfiguration());
            modelBuilder.ApplyConfiguration(new AggregationResultForwardingEntityConfiguration(_serializer));
            modelBuilder.ApplyConfiguration(new EntityConfiguration());
            modelBuilder.ApplyConfiguration(new OutgoingMessageEntityConfiguration());
            modelBuilder.ApplyConfiguration(new EnqueuedMessageEntityConfiguration());
            modelBuilder.ApplyConfiguration(new QueuedInternalCommandEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ReceivedIntegrationEventEntityConfiguration());
            modelBuilder.ApplyConfiguration(new MarketEvaluationPointEntityConfiguration());
            modelBuilder.ApplyConfiguration(new BundledMessageConfiguration());
            modelBuilder.ApplyConfiguration(new ArchivedMessageEntityConfiguration());

            modelBuilder.Entity<GenericNotificationMessage>()
                .Ignore(entity => entity.MarketActivityRecord);
            modelBuilder.Entity<ConfirmRequestChangeOfSupplierMessage>()
                .Ignore(entity => entity.MarketActivityRecord);
            modelBuilder.Entity<RejectRequestChangeOfSupplierMessage>()
                .Ignore(entity => entity.MarketActivityRecord);
            modelBuilder.Entity<AccountingPointCharacteristicsMessage>()
                .Ignore(entity => entity.MarketActivityRecord);
            modelBuilder.Entity<CharacteristicsOfACustomerAtAnApMessage>()
                .Ignore(entity => entity.MarketActivityRecord);
            modelBuilder.Entity<ConfirmRequestChangeAccountingPointCharacteristicsMessage>()
                .Ignore(entity => entity.MarketActivityRecord);
            modelBuilder.Entity<RejectRequestChangeAccountingPointCharacteristicsMessage>()
                .Ignore(entity => entity.MarketActivityRecord);
        }
    }
}
