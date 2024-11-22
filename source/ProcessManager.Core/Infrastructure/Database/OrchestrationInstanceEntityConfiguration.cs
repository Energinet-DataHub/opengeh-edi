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

using Energinet.DataHub.ProcessManagement.Core.Domain.OrchestrationDescription;
using Energinet.DataHub.ProcessManagement.Core.Domain.OrchestrationInstance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Energinet.DataHub.ProcessManagement.Core.Infrastructure.Database;

internal class OrchestrationInstanceEntityConfiguration : IEntityTypeConfiguration<OrchestrationInstance>
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

        builder.OwnsOne(
            o => o.Lifecycle,
            b =>
            {
                b.Property(l => l.State);
                b.Property(l => l.TerminationState);

                b.OwnsOne(
                    l => l.CreatedBy,
                    lb =>
                    {
                        lb.Ignore(ct => ct.Value);

                        lb.Property(ct => ct.IdentityType);
                        lb.Property(ct => ct.ActorId);
                        lb.Property(ct => ct.UserId);
                    });

                b.Property(l => l.CreatedAt);
                b.Property(l => l.ScheduledToRunAt);
                b.Property(l => l.QueuedAt);
                b.Property(l => l.StartedAt);
                b.Property(l => l.TerminatedAt);

                b.OwnsOne(
                    l => l.CanceledBy,
                    lb =>
                    {
                        lb.Ignore(ct => ct.Value);

                        lb.Property(ct => ct.IdentityType);
                        lb.Property(ct => ct.ActorId);
                        lb.Property(ct => ct.UserId);
                    });
            });

        builder.OwnsOne(
            o => o.ParameterValue,
            b =>
            {
                b.Property(l => l.SerializedParameterValue)
                    .HasColumnName(nameof(OrchestrationInstance.ParameterValue.SerializedParameterValue));
            });

        builder.OwnsMany(
            o => o.Steps,
            b =>
            {
                b.ToTable("StepInstance");

                b.HasKey(s => s.Id);
                b.Property(s => s.Id)
                    .ValueGeneratedNever()
                    .HasConversion(
                        id => id.Value,
                        dbValue => new StepInstanceId(dbValue));

                b.OwnsOne(
                    o => o.Lifecycle,
                    b =>
                    {
                        b.Property(l => l.State);
                        b.Property(l => l.TerminationState);

                        b.Property(l => l.StartedAt);
                        b.Property(l => l.TerminatedAt);

                        b.Property(l => l.CanBeSkipped);
                    });

                b.Property(s => s.Description);
                b.Property(s => s.Sequence);

                b.Property(s => s.CustomState)
                    .HasConversion(
                        state => state.Value,
                        dbValue => new StepInstanceCustomState(dbValue));

                // Relation to parent
                b.Property(s => s.OrchestrationInstanceId)
                    .HasConversion(
                        id => id.Value,
                        dbValue => new OrchestrationInstanceId(dbValue));

                b.WithOwner().HasForeignKey(s => s.OrchestrationInstanceId);
            });

        builder.Property(o => o.CustomState)
            .HasConversion(
                state => state.Value,
                dbValue => new OrchestrationInstanceCustomState(dbValue));

        // Relation to description
        builder.Property(o => o.OrchestrationDescriptionId)
            .ValueGeneratedNever()
            .HasConversion(
                id => id.Value,
                dbValue => new OrchestrationDescriptionId(dbValue));
    }
}
