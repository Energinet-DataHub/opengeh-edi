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
using Energinet.DataHub.MarketRoles.Domain.Consumers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Energinet.DataHub.MarketRoles.Infrastructure.DataAccess.Consumers
{
    public class ConsumerEntityConfiguration : IEntityTypeConfiguration<Consumer>
    {
        public void Configure(EntityTypeBuilder<Consumer> builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.ToTable("Consumers", "dbo");
            builder.HasKey(x => x.ConsumerId);
            builder.Property(x => x.ConsumerId)
                .HasColumnName("Id")
                .HasConversion(toDbValue => toDbValue.Value, fromDbValue => new ConsumerId(fromDbValue));

            builder.Property(x => x.CprNumber)
                .HasColumnName("CprNumber")
                .HasConversion(toDbValue => toDbValue == null ? null : toDbValue.Value, fromDbValue => fromDbValue == null ? null : new CprNumber(fromDbValue));
            builder.Property(x => x.CvrNumber)
                .HasColumnName("CvrNumber")
                .HasConversion(toDbValue => toDbValue == null ? null : toDbValue.Value, fromDbValue => fromDbValue == null ? null : new CvrNumber(fromDbValue));

            builder.Property<DateTime>("RowVersion")
                .HasColumnName("RowVersion")
                .HasColumnType("timestamp")
                .IsRowVersion();

            builder.Ignore(x => x.DomainEvents);
        }
    }
}
