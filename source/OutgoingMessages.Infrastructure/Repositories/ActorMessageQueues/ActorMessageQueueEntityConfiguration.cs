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

        builder.HasKey(amq => amq.Id);
        builder.Property(amq => amq.Id)
            .HasConversion(toDbValue => toDbValue.Id, fromDbValue => ActorMessageQueueId.CreateExisting(fromDbValue));

        builder.OwnsOne(amq => amq.Receiver, entityBuilder =>
        {
            entityBuilder.Property(receiver => receiver.Number).HasColumnName("ActorNumber")
                .HasConversion(toDbValue => toDbValue.Value, fromDbValue => ActorNumber.Create(fromDbValue));
            entityBuilder.Property(receiver => receiver.ActorRole).HasColumnName("ActorRole")
                .HasConversion(
                    toDbValue => toDbValue.Code,
                    fromDbValue => ActorRole.FromCode(fromDbValue));
            entityBuilder.WithOwner();
        });

        builder.HasMany<Bundle>("_bundles").WithOne().HasForeignKey(b => b.ActorMessageQueueId);

        builder.Property<string>("CreatedBy");
        builder.Property<Instant>("CreatedAt");
        builder.Property<string?>("ModifiedBy");
        builder.Property<Instant?>("ModifiedAt");
    }
}
