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
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.Transactions;
using Messaging.Domain.Transactions.MoveIn;
using Messaging.Infrastructure.Configuration.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messaging.Infrastructure.Transactions
{
    internal class MoveInTransactionEntityConfiguration : IEntityTypeConfiguration<MoveInTransaction>
    {
        private readonly ISerializer _serializer;

        public MoveInTransactionEntityConfiguration(ISerializer serializer)
        {
            _serializer = serializer;
        }

        public void Configure(EntityTypeBuilder<MoveInTransaction> builder)
        {
            builder.ToTable("MoveInTransactions", "b2b");
            builder.HasKey(x => x.TransactionId);
            builder.Property(entity => entity.TransactionId)
                .HasConversion(
                    toDbValue => toDbValue.Id,
                    fromDbValue => TransactionId.Create(fromDbValue));
            builder.Property(entity => entity.ActorProvidedId)
                .HasConversion(
                    toDbValue => toDbValue.Id,
                    fromDbValue => ActorProvidedId.Create(fromDbValue));
            builder.Property(x => x.ProcessId);
            builder.Property(x => x.EffectiveDate);
            builder.Property(x => x.MarketEvaluationPointId);
            builder.Property(x => x.CurrentEnergySupplierId);
            builder.Property(x => x.NewEnergySupplierId);
            builder.Property(x => x.ConsumerId);
            builder.Property(x => x.ConsumerIdType);
            builder.Property(x => x.ConsumerName);
            builder.Property<MoveInTransaction.MasterDataState>("_meteringPointMasterDataState")
                .HasColumnName("MeteringPointMasterDataState")
                .HasConversion(
                    toDbValue => toDbValue.ToString(),
                    fromDbValue => Enum.Parse<MoveInTransaction.MasterDataState>(fromDbValue, true));

            builder.Property<MoveInTransaction.MasterDataState>("_customerMasterDataForGridOperatorDeliveryState")
                .HasColumnName("GridOperator_MessageDeliveryState_CustomerMasterData")
                .HasConversion(
                    toDbValue => toDbValue.ToString(),
                    fromDbValue => Enum.Parse<MoveInTransaction.MasterDataState>(fromDbValue, true));

            builder.Property<MoveInTransaction.BusinessProcessState>("_businessProcessState")
                .HasColumnName("BusinessProcessState")
                .HasConversion(
                    toDbValue => toDbValue.ToString(),
                    fromDbValue => Enum.Parse<MoveInTransaction.BusinessProcessState>(fromDbValue, true));
            builder.Property<MoveInTransaction.State>("_state")
                .HasConversion(toDbValue => toDbValue.ToString(), fromDbValue => Enum.Parse<MoveInTransaction.State>(fromDbValue, true))
                .HasColumnName("State");
            builder.Property<MoveInTransaction.NotificationState>("_currentEnergySupplierNotificationState")
                .HasConversion(toDbValue => toDbValue.ToString(), fromDbValue => Enum.Parse<MoveInTransaction.NotificationState>(fromDbValue, true))
                .HasColumnName("CurrentEnergySupplierNotificationState");
            builder.Property<MoveInTransaction.NotificationState>("_gridOperatorNotificationState")
                .HasConversion(toDbValue => toDbValue.ToString(), fromDbValue => Enum.Parse<MoveInTransaction.NotificationState>(fromDbValue, true))
                .HasColumnName("GridOperatorNotificationState");
            builder.Property(x => x.StartedByMessageId);

            builder.Property<CustomerMasterData>("_customerMasterData")
                .HasColumnName("CustomerMasterData")
                .HasConversion(
                    toDbValue => _serializer.Serialize(toDbValue),
                    fromDbValue => _serializer.Deserialize<CustomerMasterData>(fromDbValue));

            builder.Property<ActorNumber>("_requestedBy")
                .HasColumnName("RequestedByActorNumber")
                .HasConversion(toDbValue => toDbValue.Value, fromDbValue => ActorNumber.Create(fromDbValue));

            builder.HasMany<OutgoingMessage>("_messages")
                .WithOne()
                .HasForeignKey("TransactionId");

            builder.Ignore(x => x.DomainEvents);
        }
    }
}
