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
using B2B.Transactions.OutgoingMessages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace B2B.Transactions.Infrastructure.DataAccess.Outgoing
{
    public class OutgoingMessageEntityConfiguration : IEntityTypeConfiguration<OutgoingMessage>
    {
        public void Configure(EntityTypeBuilder<OutgoingMessage> builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.ToTable("OutgoingMessages", "b2b");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.DocumentType);
            builder.Property(x => x.IsPublished);
            builder.Property(x => x.RecipientId);
            builder.Property(x => x.ReceiverRole);
            builder.Property(x => x.CorrelationId);
            builder.Property(x => x.OriginalMessageId);
            builder.Property(x => x.ProcessType);
            builder.Property(x => x.SenderId);
            builder.Property(x => x.SenderRole);
        }
    }
}
