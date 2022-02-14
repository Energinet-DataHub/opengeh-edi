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
using Energinet.DataHub.MarketRoles.Domain.SeedWork;
using Energinet.DataHub.MarketRoles.Infrastructure.EDI;
using Energinet.DataHub.MarketRoles.Infrastructure.LocalMessageHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Energinet.DataHub.MarketRoles.Infrastructure.DataAccess.MessageHub
{
    public class MessageHubMessageEntityConfiguration : IEntityTypeConfiguration<MessageHubMessage>
    {
        public void Configure(EntityTypeBuilder<MessageHubMessage> builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.ToTable("MessageHubMessages", "dbo");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id);

            builder.Property(x => x.Correlation)
                .HasColumnName("Correlation");

            builder.Property(x => x.Type)
                .HasColumnName("Type")
                .HasConversion(
                    toDbValue => toDbValue.Name,
                    fromDbValue => EnumerationType.FromName<DocumentType>(fromDbValue));

            builder.Property(x => x.Content)
                .HasColumnName("Content");

            builder.Property(x => x.Date)
                .HasColumnName("Date");

            builder.Property(x => x.Recipient)
                .HasColumnName("Recipient");

            builder.Property(x => x.GsrnNumber)
                .HasColumnName("GsrnNumber");
        }
    }
}
