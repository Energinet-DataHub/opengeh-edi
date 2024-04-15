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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.MarketDocuments;
using Energinet.DataHub.EDI.OutgoingMessages.Domain.Queueing.Bundels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Energinet.DataHub.EDI.OutgoingMessages.Infrastructure.OutgoingMessages.Queueing;

public class MarketDocumentEntityConfiguration : IEntityTypeConfiguration<MarketDocument>
{
    public void Configure(EntityTypeBuilder<MarketDocument> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("MarketDocuments", "dbo");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(md => md.BundleId)
            .HasConversion(toDbValue => toDbValue.Id, fromDbValue => BundleId.Create(fromDbValue));

        builder.Property(entity => entity.FileStorageReference)
            .HasConversion(
                fileStorageReference => fileStorageReference.Path,
                dbValue => new FileStorageReference(MarketDocument.FileStorageCategory, dbValue));
    }
}
