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
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.EDI.Process.Domain.Transactions.AggregatedMeasureData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Energinet.DataHub.EDI.Process.Infrastructure.Transactions.AggregatedMeasureData;

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
        builder.Property(x => x.BusinessReason)
            .HasConversion(
                value => value.Code,
                dbValue => BusinessReason.FromCodeOrUnused(dbValue));
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

        builder.Property(x => x.SettlementVersion)
            .HasConversion(
                value => value != null ? value.Code : null,
                dbValue => !string.IsNullOrWhiteSpace(dbValue) ? SettlementVersion.FromCodeOrUnused(dbValue) : null);

        builder.Property(x => x.InitiatedByMessageId)
            .HasConversion(
                toDbValue => toDbValue.Value,
                fromDbValue => MessageId.Create(fromDbValue));
        builder.Ignore(x => x.DomainEvents);
    }
}
