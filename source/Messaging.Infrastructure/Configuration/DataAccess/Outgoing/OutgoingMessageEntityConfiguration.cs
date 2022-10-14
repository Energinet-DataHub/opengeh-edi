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
using Messaging.Domain.Actors;
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messaging.Infrastructure.Configuration.DataAccess.Outgoing
{
    public class OutgoingMessageEntityConfiguration : IEntityTypeConfiguration<OutgoingMessage>
    {
        public void Configure(EntityTypeBuilder<OutgoingMessage> builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.ToTable("OutgoingMessages", "b2b");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.DocumentType)
                .HasConversion(
                    toDbValue => toDbValue.Name,
                    fromDbValue => EnumerationType.FromName<DocumentType>(fromDbValue));
            builder.Property(x => x.IsPublished);
            builder.Property(x => x.ReceiverId)
                .HasConversion(
                    toDbValue => toDbValue.Value,
                    fromDbValue => ActorNumber.Create(fromDbValue));
            builder.Property(x => x.ReceiverRole)
                .HasConversion(
                    toDbValue => toDbValue.ToString(),
                    fromDbValue => EnumerationType.FromName<MarketRole>(fromDbValue));
            builder.Property(x => x.TransactionId);
            builder.Property(x => x.ProcessType);
            builder.Property(x => x.SenderId)
                .HasConversion(
                    toDbValue => toDbValue.Value,
                    fromDbValue => ActorNumber.Create(fromDbValue));
            builder.Property(x => x.SenderRole)
                .HasConversion(
                    toDbValue => toDbValue.ToString(),
                    fromDbValue => EnumerationType.FromName<MarketRole>(fromDbValue));
            builder.Property(x => x.MarketActivityRecordPayload);
        }
    }
}
