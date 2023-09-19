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
using Domain.Actors;
using Domain.OutgoingMessages;
using Domain.Transactions;
using Domain.Transactions.AggregatedMeasureData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Transactions.AggregatedMeasureData;

internal sealed class AggregatedMeasureDataProcessEntityConfiguration : IEntityTypeConfiguration<AggregatedMeasureDataProcess>
{
    public void Configure(EntityTypeBuilder<AggregatedMeasureDataProcess> builder)
    {
        builder.ToTable("AggregatedMeasureDataProcesses", "dbo");
        builder.HasKey(x => x.ProcessId);
        builder.Property(x => x.ProcessId)
            .HasConversion(
                toDbValue => toDbValue.Id,
                fromDbValue => ProcessId.Create(fromDbValue));
        builder.Property(x => x.BusinessTransactionId)
            .HasConversion(
                toDbValue => toDbValue.Id,
                fromDbValue => BusinessTransactionId.Create(fromDbValue));
        builder.Property(x => x.MeteringPointType);
        builder.Property(x => x.SettlementMethod);
        builder.Property(x => x.StartOfPeriod);
        builder.Property(x => x.EndOfPeriod);
        builder.Property(x => x.MeteringGridAreaDomainId);
        builder.Property(x => x.EnergySupplierId);
        builder.Property(x => x.BalanceResponsibleId);
        builder.Property(x => x.BusinessReason);
        builder.Property(x => x.ResponseData);
        builder.Property(x => x.RequestedByActorId)
            .HasConversion(
                toDbValue => toDbValue.Value,
                fromDbValue => ActorNumber.Create(fromDbValue));
        builder.Property(x => x.RequestedByActorRoleCode);

        builder.Property<AggregatedMeasureDataProcess.State>("_state")
            .HasConversion(
                toDbValue => toDbValue.ToString(),
                fromDbValue => Enum.Parse<AggregatedMeasureDataProcess.State>(fromDbValue, true))
            .HasColumnName("State");

        builder.HasMany<OutgoingMessage>("_messages")
            .WithOne()
            .HasForeignKey("ProcessId");

        builder.Ignore(x => x.DomainEvents);
    }
}
