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
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodaTime;

namespace Energinet.DataHub.EDI.MasterData.Infrastructure.ProcessDelegation;

public class ProcessDelegationEntityConfiguration : IEntityTypeConfiguration<Domain.ProcessDelegations.ProcessDelegation>
{
    public void Configure(EntityTypeBuilder<Domain.ProcessDelegations.ProcessDelegation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("ProcessDelegation", "dbo");
        builder.HasKey("_id");
        builder.Property<Guid>("_id").HasColumnName("Id");

        builder.Property(entity => entity.DelegatedByActorNumber)
            .HasConversion(actorNumber => actorNumber.Value, dbValue => ActorNumber.Create(dbValue));
        builder.Property(entity => entity.DelegatedByActorRole)
            .HasConversion(actorRole => actorRole.Code, dbValue => ActorRole.FromCode(dbValue));
        builder.Property(entity => entity.DelegatedToActorNumber)
            .HasConversion(actorNumber => actorNumber.Value, dbValue => ActorNumber.Create(dbValue));
        builder.Property(entity => entity.DelegatedToActorRole)
            .HasConversion(actorRole => actorRole.Code, dbValue => ActorRole.FromCode(dbValue));
        builder.Property(entity => entity.GridAreaCode);
        builder.Property(entity => entity.DelegatedProcess)
            .HasConversion(delegatedProcess => delegatedProcess.Name, dbValue => ProcessType.FromName(dbValue));
        builder.Property(entity => entity.SequenceNumber);
        builder.Property(entity => entity.StartsAt)
            .HasConversion(startsAt => startsAt.ToDateTimeOffset(), dbValue => Instant.FromDateTimeOffset(dbValue))
            .HasColumnName("Start");
        builder.Property(entity => entity.StopsAt)
            .HasConversion(stopsAt => stopsAt.ToDateTimeOffset(), dbValue => Instant.FromDateTimeOffset(dbValue))
            .HasColumnName("End");
    }
}
