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

public class OrchestrationDescriptionEntityConfiguration : IEntityTypeConfiguration<OrchestrationDescription>
{
    public void Configure(EntityTypeBuilder<OrchestrationDescription> builder)
    {
        builder.ToTable("OrchestrationDescription");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id)
            .ValueGeneratedNever()
            .HasConversion(
                id => id.Value,
                dbValue => new OrchestrationDescriptionId(dbValue));

        builder.Property(o => o.Name);
        builder.Property(o => o.Version);
        builder.Property(o => o.CanBeScheduled);
        builder.Property(o => o.HostName);
        builder.Property(o => o.IsEnabled);

        // TODO: Add parameter definition; sry I had to change it :)
    }
}
