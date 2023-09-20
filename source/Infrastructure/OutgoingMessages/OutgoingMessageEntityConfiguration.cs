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
using Domain.Actors;
using Domain.Common;
using Domain.Documents;
using Domain.OutgoingMessages;
using Domain.OutgoingMessages.MoveIn.AccountingPointCharacteristics;
using Domain.OutgoingMessages.MoveIn.CharacteristicsOfACustomerAtAnAp;
using Domain.OutgoingMessages.MoveIn.ConfirmRequestChangeAccountingPointCharacteristics;
using Domain.OutgoingMessages.MoveIn.ConfirmRequestChangeOfSupplier;
using Domain.OutgoingMessages.MoveIn.GenericNotification;
using Domain.OutgoingMessages.MoveIn.RejectRequestChangeAccountingPointCharacteristics;
using Domain.OutgoingMessages.MoveIn.RejectRequestChangeOfSupplier;
using Domain.OutgoingMessages.NotifyAggregatedMeasureData;
using Domain.OutgoingMessages.Queueing;
using Domain.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.OutgoingMessages
{
    public class OutgoingMessageEntityConfiguration : IEntityTypeConfiguration<OutgoingMessage>
    {
        public void Configure(EntityTypeBuilder<OutgoingMessage> builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.ToTable("OutgoingMessages", "dbo");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.DocumentType)
                .HasConversion(
                    toDbValue => toDbValue.Name,
                    fromDbValue => EnumerationType.FromName<DocumentType>(fromDbValue));
            builder.Property(x => x.IsPublished);
            builder.Property(x => x.ReceiverId)
                .HasConversion(
                    toDbValue => toDbValue.Value,
                    fromDbValue => ActorNumber.Create(fromDbValue));
            builder.Property(x => x.ReceiverRole)
                .HasConversion(
                    toDbValue => toDbValue.ToString(),
                    fromDbValue => EnumerationType.FromName<MarketRole>(fromDbValue));
            builder.Property(x => x.ProcessId)
                .HasConversion(
                    toDbValue => toDbValue.Id,
                    fromDbValue => ProcessId.Create(fromDbValue));
            builder.Property(x => x.BusinessReason);
            builder.Property(x => x.SenderId)
                .HasConversion(
                    toDbValue => toDbValue.Value,
                    fromDbValue => ActorNumber.Create(fromDbValue));
            builder.Property(x => x.SenderRole)
                .HasConversion(
                    toDbValue => toDbValue.ToString(),
                    fromDbValue => EnumerationType.FromName<MarketRole>(fromDbValue));
            builder.Property(x => x.MessageRecord);
            builder.Property(x => x.AssignedBundleId).HasConversion(
                toDbValue => toDbValue == null ? Guid.Empty : toDbValue.Id,
                fromDbValue => fromDbValue == Guid.Empty ? null : BundleId.Create(fromDbValue));

            builder.Ignore(x => x.Receiver);

            builder
                .HasDiscriminator<string>("Discriminator")
                .HasValue<OutgoingMessage>(nameof(OutgoingMessage))
                .HasValue<ConfirmRequestChangeOfSupplierMessage>(DocumentType.ConfirmRequestChangeOfSupplier.Name)
                .HasValue<RejectRequestChangeOfSupplierMessage>(DocumentType.RejectRequestChangeOfSupplier.Name)
                .HasValue<GenericNotificationMessage>(DocumentType.GenericNotification.Name)
                .HasValue<AccountingPointCharacteristicsMessage>(DocumentType.AccountingPointCharacteristics.Name)
                .HasValue<CharacteristicsOfACustomerAtAnApMessage>(DocumentType.CharacteristicsOfACustomerAtAnAP.Name)
                .HasValue<ConfirmRequestChangeAccountingPointCharacteristicsMessage>(DocumentType.ConfirmRequestChangeAccountingPointCharacteristics.Name)
                .HasValue<RejectRequestChangeAccountingPointCharacteristicsMessage>(DocumentType.RejectRequestChangeAccountingPointCharacteristics.Name)
                .HasValue<AggregationResultMessage>(DocumentType.NotifyAggregatedMeasureData.Name)
                .IsComplete(false);
        }
    }
}
