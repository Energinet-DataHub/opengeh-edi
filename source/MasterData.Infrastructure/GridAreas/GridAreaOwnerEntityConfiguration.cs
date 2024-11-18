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
using Energinet.DataHub.EDI.MasterData.Domain.GridAreaOwners;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Energinet.DataHub.EDI.MasterData.Infrastructure.GridAreas;

public class GridAreaOwnerEntityConfiguration : IEntityTypeConfiguration<GridAreaOwner>
{
    public void Configure(EntityTypeBuilder<GridAreaOwner> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("GridAreaOwner", "dbo");
        builder.HasKey("_id");
        builder.Property<Guid>("_id").HasColumnName("Id");
        builder.Property(entity => entity.GridAreaCode);
        builder.Property(entity => entity.ValidFrom);
        builder.Property(entity => entity.SequenceNumber);
        builder.Property(receiver => receiver.GridAreaOwnerActorNumber)
            .HasConversion(toDbValue => toDbValue.Value, fromDbValue => ActorNumber.Create(fromDbValue));
    }
}
