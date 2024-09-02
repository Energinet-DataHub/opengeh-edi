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

using Energinet.DataHub.EDI.Outbox.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodaTime;

namespace Energinet.DataHub.EDI.Outbox.Infrastructure;

internal sealed class OutboxEntityConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("Outbox", "dbo");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .ValueGeneratedNever()
            .HasConversion(
                id => id.Id,
                dbValue => new OutboxMessageId(dbValue));

        builder.Property(o => o.Type);
        builder.Property(o => o.Payload);

        builder.Property(o => o.ProcessingAt);
        builder.Property(o => o.PublishedAt);
        builder.Property(o => o.FailedAt);
        builder.Property(o => o.ErrorMessage);
        builder.Property(o => o.ErrorCount);
        builder.Property(o => o.RowVersion)
            .IsRowVersion();

        builder.Property<string>("CreatedBy");
        builder.Property<Instant>("CreatedAt");
        builder.Property<string?>("ModifiedBy");
        builder.Property<Instant?>("ModifiedAt");
    }
}
