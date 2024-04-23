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
using Actor = Energinet.DataHub.EDI.MasterData.Domain.Actors.Actor;

namespace Energinet.DataHub.EDI.MasterData.Infrastructure.Actors;

public class ActorEntityConfiguration : IEntityTypeConfiguration<Actor>
{
    public void Configure(EntityTypeBuilder<Actor> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Actor", "dbo");
        builder.HasKey("_id");
        builder.Property<Guid>("_id").HasColumnName("Id");
        builder.Property(receiver => receiver.ActorNumber).HasColumnName("ActorNumber")
            .HasConversion(toDbValue => toDbValue.Value, fromDbValue => ActorNumber.Create(fromDbValue));
        builder.Property(entity => entity.ExternalId);
    }
}
