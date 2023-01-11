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
using System.IO;
using System.Linq;
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.OutgoingMessages.Peek;
using Messaging.Domain.SeedWork;
using Messaging.Infrastructure.Configuration.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messaging.Infrastructure.OutgoingMessages.Peek;

public class BundledMessageConfiguration : IEntityTypeConfiguration<BundledMessage>
{
    public void Configure(EntityTypeBuilder<BundledMessage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("BundledMessages", "B2B");

        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id)
            .HasColumnName("Id")
            .HasConversion(toDbValue => toDbValue.Value, fromDbValue => BundledMessageId.From(fromDbValue));
        builder.Property(entity => entity.Category)
            .HasColumnName("MessageCategory")
            .HasConversion(
                toDbValue => toDbValue.Name,
                fromDbValue => EnumerationType.FromName<MessageCategory>(fromDbValue));
        builder.Property(entity => entity.GeneratedDocument)
            .HasColumnName("GeneratedDocument")
            .HasConversion(
                toDbValue => ((MemoryStream)toDbValue).ToArray(),
                fromDbValue => new MemoryStream(fromDbValue));
        builder.Property(entity => entity.MessageIdsIncluded)
            .HasColumnName("MessageIdsIncluded")
            .HasConversion(
                toDbValue => string.Join(",", toDbValue),
                fromDbValue => fromDbValue.Split(",", StringSplitOptions.None)
                    .Select(value => Guid.Parse(value)));
        builder.Property(entity => entity.ReceiverNumber)
            .HasColumnName("ReceiverNumber")
            .HasConversion(toDbValue => toDbValue.Value, fromDbValue => ActorNumber.Create(fromDbValue));
    }
}
