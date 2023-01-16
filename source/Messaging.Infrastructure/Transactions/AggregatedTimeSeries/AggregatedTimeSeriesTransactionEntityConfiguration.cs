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
using Messaging.Domain.OutgoingMessages;
using Messaging.Domain.Transactions;
using Messaging.Domain.Transactions.AggregatedTimeSeries;
using Messaging.Infrastructure.Configuration.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messaging.Infrastructure.Transactions.AggregatedTimeSeries;

internal class AggregatedTimeSeriesTransactionEntityConfiguration : IEntityTypeConfiguration<AggregatedTimeSeriesTransaction>
{
    private readonly ISerializer _serializer;

    internal AggregatedTimeSeriesTransactionEntityConfiguration(ISerializer serializer)
    {
        _serializer = serializer;
    }

    public void Configure(EntityTypeBuilder<AggregatedTimeSeriesTransaction> builder)
    {
        builder.ToTable("AggregatedTimeSeriesTransactions", "b2b");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id)
            .HasConversion(toDbValue => Guid.Parse(toDbValue.Id), fromDbValue => TransactionId.Create(fromDbValue.ToString()));
        builder.HasMany<OutgoingMessage>("_messages")
            .WithOne()
            .HasForeignKey("TransactionId");
        builder.Ignore(entity => entity.DomainEvents);
    }
}
