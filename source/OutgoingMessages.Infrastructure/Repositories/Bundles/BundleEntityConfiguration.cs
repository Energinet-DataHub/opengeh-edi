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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.ActorMessagesQueues;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.Bundles;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Models.OutgoingMessages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.Repositories.Bundles;

public class BundleEntityConfiguration : IEntityTypeConfiguration<Bundle>
{
    public void Configure(EntityTypeBuilder<Bundle> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Bundles", "dbo");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id)
            .HasConversion(toDbValue => toDbValue.Id, fromDbValue => BundleId.Create(fromDbValue));

        builder.Property<ActorMessageQueueId>(b => b.ActorMessageQueueId)
            .HasConversion(toDbValue => toDbValue.Id, fromDbValue => ActorMessageQueueId.CreateExisting(fromDbValue));

        builder.Property(b => b.Created);
        builder.Property(b => b.ClosedAt);
        builder.Property(b => b.PeekedAt);
        builder.Property(b => b.DequeuedAt);

        builder.Property(b => b.MessageId)
            .HasConversion(toDb => toDb.Value, fromDb => MessageId.Create(fromDb));

        builder.Property<DocumentType>(b => b.DocumentTypeInBundle)
            .HasConversion(toDbValue => toDbValue.Name, fromDbValue => EnumerationType.FromName<DocumentType>(fromDbValue));

        builder.Property<BusinessReason>(b => b.BusinessReason)
            .HasConversion(toDbValue => toDbValue.Name, fromDbValue => EnumerationType.FromName<BusinessReason>(fromDbValue));

        builder.Property<int>("_messageCount").HasColumnName("MessageCount");
        builder.Property<int>("_maxNumberOfMessagesInABundle").HasColumnName("MaxMessageCount");

        builder.Property(b => b.RelatedToMessageId)
            .HasConversion(
                toDbValue => toDbValue != null ? toDbValue.Value.Value : null,
                fromDbValue => fromDbValue != null ? MessageId.Create(fromDbValue) : null);

        builder.HasMany<OutgoingMessage>().WithOne().HasForeignKey(o => o.AssignedBundleId);
    }
}
