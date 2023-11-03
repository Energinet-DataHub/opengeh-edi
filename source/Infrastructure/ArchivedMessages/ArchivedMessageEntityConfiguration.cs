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
using Energinet.DataHub.EDI.Domain.ArchivedMessages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Energinet.DataHub.EDI.Infrastructure.ArchivedMessages;

public class ArchivedMessageEntityConfiguration : IEntityTypeConfiguration<ArchivedMessage>
{
    public void Configure(EntityTypeBuilder<ArchivedMessage> builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        builder.ToTable("ArchivedMessages", "dbo");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedNever();
        builder.Property(x => x.DocumentType);
        builder.Property(x => x.SenderNumber);
        builder.Property(x => x.ReceiverNumber);
        builder.Property(x => x.CreatedAt);
        builder.Property(x => x.BusinessReason);
        builder.Property(x => x.Document)
            .HasColumnName("Document")
            .HasConversion(
                toDbValue => ((MemoryStream)toDbValue).ToArray(),
                fromDbValue => new MemoryStream(fromDbValue));
        builder.Property(x => x.MessageId);
    }
}
