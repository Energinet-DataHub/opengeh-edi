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

using Domain.Actors;
using Domain.OutgoingMessages;
using Domain.SeedWork;
using Domain.Transactions;
using Domain.Transactions.Aggregations;
using Infrastructure.Configuration.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Transactions.Aggregations;

internal sealed class AggregationResultForwardingEntityConfiguration : IEntityTypeConfiguration<AggregationResultForwarding>
{
    private readonly ISerializer _serializer;

    internal AggregationResultForwardingEntityConfiguration(ISerializer serializer)
    {
        _serializer = serializer;
    }

    public void Configure(EntityTypeBuilder<AggregationResultForwarding> builder)
    {
        builder.ToTable("AggregatedTimeSeriesTransactions", "dbo");
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
