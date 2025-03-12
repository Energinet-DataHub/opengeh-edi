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
using Energinet.DataHub.EDI.MasterData.Domain.ActorCertificates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Energinet.DataHub.EDI.MasterData.Infrastructure.ActorCertificate;

public class ActorCertificateEntityConfiguration : IEntityTypeConfiguration<Domain.ActorCertificates.ActorCertificate>
{
    public void Configure(EntityTypeBuilder<Domain.ActorCertificates.ActorCertificate> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ActorCertificate", "dbo");
        builder.HasKey("_id");
        builder.Property<Guid>("_id").HasColumnName("Id");
        builder.Property(receiver => receiver.ActorNumber)
            .HasConversion(actorNumber => actorNumber.Value, dbValue => ActorNumber.Create(dbValue));
        builder.Property(receiver => receiver.ActorRole)
            .HasConversion(actorRole => actorRole.Code, dbValue => ActorRole.FromCode(dbValue));
        builder.Property(entity => entity.Thumbprint)
            .HasConversion(thumbprint => thumbprint.Thumbprint, dbValue => new CertificateThumbprint(dbValue));
        builder.Property(entity => entity.ValidFrom);
        builder.Property(entity => entity.SequenceNumber);
    }
}
