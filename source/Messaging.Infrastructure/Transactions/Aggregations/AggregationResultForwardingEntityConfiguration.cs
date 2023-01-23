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
using Messaging.Domain.Transactions;
using Messaging.Domain.Transactions.Aggregations;
using Messaging.Infrastructure.Configuration.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messaging.Infrastructure.Transactions.Aggregations;

internal class AggregationResultForwardingEntityConfiguration : IEntityTypeConfiguration<AggregationResultForwarding>
{
    private readonly ISerializer _serializer;

    internal AggregationResultForwardingEntityConfiguration(ISerializer serializer)
    {
        _serializer = serializer;
    }

    public void Configure(EntityTypeBuilder<AggregationResultForwarding> builder)
    {
        builder.ToTable("AggregatedTimeSeriesTransactions", "b2b");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id)
            .HasConversion(toDbValue => toDbValue.Id, fromDbValue => TransactionId.Create(fromDbValue));
        builder.Property<ProcessType>("_processType")
            .HasColumnName("ProcessType")
            .HasConversion(toDbValue => toDbValue.Name, fromDbValue => EnumerationType.FromName<ProcessType>(fromDbValue));
        builder.Property<ActorNumber>("_receivingActor")
            .HasColumnName("ReceivingActor")
            .HasConversion(toDbValue => toDbValue.Value, fromDbValue => ActorNumber.Create(fromDbValue));
        builder.Property<MarketRole>("_receivingActorRole")
            .HasColumnName("ReceivingActorRole")
            .HasConversion(toDbValue => toDbValue.Name, fromDbValue => EnumerationType.FromName<MarketRole>(fromDbValue));
        builder.HasMany<OutgoingMessage>("_messages")
            .WithOne()
            .HasForeignKey("TransactionId");
        builder.Ignore(entity => entity.DomainEvents);
    }
}
