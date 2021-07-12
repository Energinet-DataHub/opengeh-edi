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
using Energinet.DataHub.MarketRoles.Application.Common.Processing;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Energinet.DataHub.MarketRoles.Infrastructure.DataAccess.ProcessManagers
{
    public class ProcessManagerEntityConfiguration : IEntityTypeConfiguration<ProcessManager>
    {
        public void Configure(EntityTypeBuilder<ProcessManager> builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            builder.ToTable("ProcessManagers", "dbo");
            builder.HasDiscriminator<string>("Type");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.EffectiveDate)
                .HasColumnName("EffectiveDate");
            builder.Property(x => x.BusinessProcessId)
                .HasColumnName("BusinessProcessId")
                .HasConversion(toDbValue => toDbValue.Value, fromDbValue => new BusinessProcessId(fromDbValue));

            builder.Ignore(x => x.CommandsToSend);
        }
    }
}
