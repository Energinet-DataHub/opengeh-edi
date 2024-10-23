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

using Energinet.DataHub.ProcessManagement.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Energinet.DataHub.ProcessManagement.Core.Infrastructure.Database;

public class OrchestrationInstanceEntityConfiguration : IEntityTypeConfiguration<OrchestrationInstance>
{
    public void Configure(EntityTypeBuilder<OrchestrationInstance> builder)
    {
        builder.ToTable("OrchestrationInstance");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id)
            .ValueGeneratedNever()
            .HasConversion(
                id => id.Value,
                dbValue => new OrchestrationInstanceId(dbValue));

        builder.Property(o => o.CreatedAt);
        builder.Property(o => o.ScheduledAt);
        builder.Property(o => o.StartedAt);
        builder.Property(o => o.ChangedAt);
        builder.Property(o => o.CompletedAt);

        // TODO: Add parameter value; sry I had to change it :)

        builder.OwnsMany(
            o => o.Steps,
            b =>
            {
                b.ToTable("OrchestrationInstanceStep");

                b.HasKey(s => s.Id);
                b.Property(s => s.Id)
                    .ValueGeneratedNever()
                    .HasConversion(
                        id => id.Value,
                        dbValue => new OrchestrationStepInstanceId(dbValue));

                b.Property(s => s.Description);

                b.Property(s => s.StartedAt);
                b.Property(s => s.ChangedAt);
                b.Property(s => s.CompletedAt);
                b.Property(s => s.Sequence);

                b.Property(s => s.DependsOn)
                    .HasConversion(
                        id => id != null ? id.Value : (Guid?)null,
                        dbValue => dbValue != null ? new OrchestrationStepInstanceId(dbValue.Value) : null);

                b.Property(s => s.State)
                    .HasConversion(
                        state => state != null ? state.Value : null,
                        dbValue => dbValue != null ? new OrchestrationStepInstanceState(dbValue) : null);

                b.Property(s => s.OrchestrationInstanceId)
                    .HasConversion(
                        id => id.Value,
                        dbValue => new OrchestrationInstanceId(dbValue));
                b.WithOwner().HasForeignKey(s => s.OrchestrationInstanceId);
            });

        builder.Property(o => o.OrchestrationDescriptionId)
            .ValueGeneratedNever()
            .HasConversion(
                id => id.Value,
                dbValue => new OrchestrationDescriptionId(dbValue));
    }
}
