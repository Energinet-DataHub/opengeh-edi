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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Energinet.DataHub.EDI.OutgoingMessages.Interfaces.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.OutgoingMessages
{
    public class OutgoingMessageEntityConfiguration : IEntityTypeConfiguration<OutgoingMessage>
    {
        public void Configure(EntityTypeBuilder<OutgoingMessage> builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.ToTable("OutgoingMessages", "dbo");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .ValueGeneratedNever()
                .HasConversion(
                    outgoingMessageId => outgoingMessageId.Value,
                    dbValue => OutgoingMessageId.CreateFromExisting(dbValue));

            builder.Property(x => x.DocumentType)
                .HasConversion(
                    toDbValue => toDbValue.Name,
                    fromDbValue => EnumerationType.FromName<DocumentType>(fromDbValue));

            builder.Property(x => x.ProcessId);

            builder.Property(x => x.BusinessReason);

            builder.Property(x => x.GridAreaCode);

            builder.Property(x => x.EventId)
                .HasConversion(
                    eventId => eventId.Value,
                    dbValue => EventId.From(dbValue));

            builder.Property(x => x.SenderId)
                .HasConversion(
                    toDbValue => toDbValue.Value,
                    fromDbValue => ActorNumber.Create(fromDbValue));

            builder.Property(x => x.SenderRole)
                .HasConversion(
                    toDbValue => toDbValue.Code,
                    fromDbValue => ActorRole.FromCode(fromDbValue));

            builder.Property(x => x.AssignedBundleId).HasConversion(
                toDbValue => toDbValue == null ? Guid.Empty : toDbValue.Id,
                fromDbValue => fromDbValue == Guid.Empty ? null : BundleId.Create(fromDbValue));

            builder.Ignore(x => x.Receiver);

            builder.Property(om => om.FileStorageReference)
                .HasConversion(
                    fileStorageReference => fileStorageReference.Path,
                    dbValue => new FileStorageReference(OutgoingMessage.FileStorageCategory, dbValue));
            builder.Property(x => x.RelatedToMessageId)
                .HasConversion(
                    toDbValue => toDbValue != null ? toDbValue.Value : null,
                    fromDbValue => fromDbValue != null ? MessageId.Create(fromDbValue) : null);
            builder.Property(x => x.MessageCreatedFromProcess)
                .HasConversion(
                    toDbValue => toDbValue.Name,
                    fromDbValue => ProcessType.FromName(fromDbValue));

            builder.OwnsOne(
                o => o.DocumentReceiver,
                r =>
                {
                    r.Property(x => x.Number)
                        .HasConversion(
                            toDbValue => toDbValue.Value,
                            fromDbValue => ActorNumber.Create(fromDbValue))
                        .HasColumnName("DocumentReceiverNumber");
                    r.Property(x => x.ActorRole)
                        .HasConversion(
                            toDbValue => toDbValue.Code,
                            fromDbValue => ActorRole.FromCode(fromDbValue))
                        .HasColumnName("DocumentReceiverRole");
                });

            builder.OwnsOne(
                o => o.Receiver,
                r =>
                {
                    r.Property(x => x.Number)
                        .HasConversion(
                            toDbValue => toDbValue.Value,
                            fromDbValue => ActorNumber.Create(fromDbValue))
                        .HasColumnName("ReceiverNumber");
                    r.Property(x => x.ActorRole)
                        .HasConversion(
                            toDbValue => toDbValue.Code,
                            fromDbValue => ActorRole.FromCode(fromDbValue))
                        .HasColumnName("ReceiverRole");
                });

            builder.Property(x => x.CreatedAt);
            builder.Property<string>("CreatedBy");
            builder.Property<string?>("ModifiedBy");
            builder.Property<Instant?>("ModifiedAt");
        }
    }
}
