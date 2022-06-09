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

using Messaging.Application.Transactions.MoveIn;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messaging.Infrastructure.Transactions
{
    internal class TransactionEntityConfiguration : IEntityTypeConfiguration<MoveInTransaction>
    {
        public void Configure(EntityTypeBuilder<MoveInTransaction> builder)
        {
            builder.ToTable("Transactions", "b2b");
            builder.HasKey(x => x.TransactionId);
            builder.Property(x => x.ProcessId);
            builder.Property(x => x.EffectiveDate);
            builder.Property(x => x.MarketEvaluationPointId);
            builder.Property(x => x.CurrentEnergySupplierId);
            builder.Property<bool>("_started")
                .HasColumnName("Started");
        }
    }
}
