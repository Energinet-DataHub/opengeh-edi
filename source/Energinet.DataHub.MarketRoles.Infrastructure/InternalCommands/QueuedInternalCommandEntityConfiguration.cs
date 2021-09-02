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
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Energinet.DataHub.MarketRoles.Infrastructure.InternalCommands
{
    public class QueuedInternalCommandEntityConfiguration : IEntityTypeConfiguration<QueuedInternalCommand>
    {
        public void Configure(EntityTypeBuilder<QueuedInternalCommand> builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.ToTable("QueuedInternalCommands", "dbo");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Data)
                .HasColumnName("Data");
            builder.Property(x => x.Type)
                .HasColumnName("Type");
            builder.Property(x => x.CreationDate)
                .HasColumnName("CreationDate");
            builder.Property(x => x.DispatchedDate)
                .HasColumnName("DispatchedDate");
            builder.Property(x => x.SequenceId)
                .HasColumnName("SequenceId");
            builder.Property(x => x.ProcessedDate)
                .HasColumnName("ProcessedDate");
            builder.Property(x => x.ScheduleDate)
                .HasColumnName("ScheduleDate");
            builder.Property(x => x.Correlation)
                .HasColumnName("Correlation");
        }
    }
}
