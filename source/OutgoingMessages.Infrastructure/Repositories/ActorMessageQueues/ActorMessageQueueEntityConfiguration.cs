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
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodaTime;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Repositories.ActorMessageQueues;

public class ActorMessageQueueEntityConfiguration : IEntityTypeConfiguration<ActorMessageQueue>
{
    public void Configure(EntityTypeBuilder<ActorMessageQueue> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ActorMessageQueues", "dbo");
        builder.HasKey("_id");
        builder.Property<Guid>("_id").HasColumnName("Id");
        builder.OwnsOne<Receiver>("Receiver", entityBuilder =>
        {
            entityBuilder.Property(receiver => receiver.Number).HasColumnName("ActorNumber")
                .HasConversion(toDbValue => toDbValue.Value, fromDbValue => ActorNumber.Create(fromDbValue));
            entityBuilder.Property(receiver => receiver.ActorRole).HasColumnName("ActorRole")
                .HasConversion(
                    toDbValue => toDbValue.Code,
                    fromDbValue => ActorRole.FromCode(fromDbValue));
            entityBuilder.WithOwner();
        });
        builder.OwnsMany<Bundle>("_bundles", navigationBuilder =>
        {
            navigationBuilder.ToTable("Bundles", "dbo");
            navigationBuilder.HasKey("Id");
            navigationBuilder.Property<BundleId>("Id").HasColumnName("Id")
                .HasConversion(toDbValue => toDbValue.Id, fromDbValue => BundleId.Create(fromDbValue));
            navigationBuilder.Property<bool>("IsClosed").HasColumnName("IsClosed");
            navigationBuilder.Property<bool>("IsDequeued").HasColumnName("IsDequeued");
            navigationBuilder.Property<GloriousEbixUuid>("MessageId")
                .HasColumnName("MessageId")
                .HasConversion(toDb => toDb.Id, fromDb => GloriousEbixUuid.From(fromDb));
            navigationBuilder.Property<DocumentType>("DocumentTypeInBundle").HasColumnName("DocumentTypeInBundle")
                .HasConversion(toDbValue => toDbValue.Name, fromDbValue => EnumerationType.FromName<DocumentType>(fromDbValue));
            navigationBuilder.Property<BusinessReason>("BusinessReason").HasColumnName("BusinessReason")
                .HasConversion(toDbValue => toDbValue.Name, fromDbValue => EnumerationType.FromName<BusinessReason>(fromDbValue));
            navigationBuilder.Property<int>("_messageCount").HasColumnName("MessageCount");
            navigationBuilder.Property<int>("_maxNumberOfMessagesInABundle").HasColumnName("MaxMessageCount");
            navigationBuilder.Property<Instant>("Created").HasColumnName("Created");
            navigationBuilder.Property<MessageId?>("RelatedToMessageId").HasColumnName("RelatedToMessageId")
                .HasConversion(
                    toDbValue => toDbValue != null ? toDbValue.Value : null,
                    fromDbValue => fromDbValue != null ? MessageId.Create(fromDbValue) : null);
            navigationBuilder.WithOwner().HasForeignKey("ActorMessageQueueId");
        });

        builder.Property<string>("CreatedBy");
        builder.Property<Instant>("CreatedAt");
        builder.Property<string?>("ModifiedBy");
        builder.Property<Instant?>("ModifiedAt");
    }
}
