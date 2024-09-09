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

using Energinet.DataHub.EDI.BuildingBlocks.Domain.Models;
using Energinet.DataHub.EDI.Process.Domain.Transactions;
using Energinet.DataHub.EDI.Process.Domain.Transactions.WholesaleServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodaTime;

namespace Energinet.DataHub.EDI.Process.Infrastructure.Transactions.WholesaleServices;

internal sealed class WholesaleServicesProcessEntityConfiguration : IEntityTypeConfiguration<WholesaleServicesProcess>
{
    public void Configure(EntityTypeBuilder<WholesaleServicesProcess> builder)
    {
        builder.ToTable("WholesaleServicesProcesses", "dbo");
        builder.HasKey(x => x.ProcessId);
        builder.Property(x => x.ProcessId)
            .HasConversion(
                toDbValue => toDbValue.Id,
                fromDbValue => ProcessId.Create(fromDbValue));
        builder.Property(x => x.BusinessTransactionId)
            .HasConversion(
                toDbValue => toDbValue.Value,
                fromDbValue => TransactionId.From(fromDbValue));
        builder.Property(x => x.StartOfPeriod);
        builder.Property(x => x.EndOfPeriod);
        builder.Property(x => x.RequestedGridArea);
        builder.Property(x => x.ChargeOwner);
        builder.Property(x => x.Resolution);
        builder.Property(x => x.EnergySupplierId);
        builder.Property(x => x.BusinessReason)
            .HasConversion(
                value => value.Code,
                dbValue => BusinessReason.FromCodeOrUnused(dbValue));

        builder.OwnsOne(
            x => x.RequestedByActor,
            actor =>
            {
                actor.Property(a => a.ActorNumber)
                    .HasColumnName("RequestedByActorNumber")
                    .HasConversion(
                        actorNumber => actorNumber.Value,
                        dbValue => ActorNumber.Create(dbValue));

                actor.Property(a => a.ActorRole)
                    .HasColumnName("RequestedByActorRole")
                    .HasConversion(
                        actorRole => actorRole.Code,
                        dbValue => ActorRole.FromCode(dbValue));
            });

        builder.OwnsOne(
            x => x.OriginalActor,
            actor =>
            {
                actor.Property(a => a.ActorNumber)
                    .HasColumnName("OriginalActorNumber")
                    .HasConversion(
                        actorNumber => actorNumber.Value,
                        dbValue => ActorNumber.Create(dbValue));

                actor.Property(a => a.ActorRole)
                    .HasColumnName("OriginalActorRole")
                    .HasConversion(
                        actorRole => actorRole.Code,
                        dbValue => ActorRole.FromCode(dbValue));
            });

        builder.Property<WholesaleServicesProcess.State>("_state")
            .HasConversion(
                toDbValue => toDbValue.ToString(),
                fromDbValue => Enum.Parse<WholesaleServicesProcess.State>(fromDbValue, true))
            .HasColumnName("State");

        builder.Property(x => x.SettlementVersion)
            .HasConversion(
                value => value != null ? value.Code : null,
                dbValue => !string.IsNullOrWhiteSpace(dbValue) ? SettlementVersion.FromCodeOrUnused(dbValue) : null);

        builder.Property(x => x.InitiatedByMessageId)
            .HasConversion(
                toDbValue => toDbValue.Value,
                fromDbValue => MessageId.Create(fromDbValue));

        builder.OwnsMany(
            x => x.ChargeTypes,
            navigationBuilder =>
            {
                navigationBuilder.ToTable("WholesaleServicesProcessChargeTypes", "dbo");
                navigationBuilder.HasKey("ChargeTypeId");
                navigationBuilder.Property<ChargeTypeId>("ChargeTypeId").HasColumnName("ChargeTypeId")
                    .HasConversion(
                        toDbValue => toDbValue.Id,
                        fromDbValue => ChargeTypeId.Create(fromDbValue));
                navigationBuilder.Property<string?>(x => x.Id);
                navigationBuilder.Property<string?>(x => x.Type);
                navigationBuilder.WithOwner().HasForeignKey("WholesaleServicesProcessId");
            });

        builder.OwnsMany<WholesaleServicesProcessGridArea>(
            "_gridAreas",
            navigationBuilder =>
            {
                navigationBuilder.ToTable("WholesaleServicesProcessGridAreas", "dbo");
                navigationBuilder.WithOwner().HasForeignKey(x => x.WholesaleServicesProcessId);
                navigationBuilder.HasKey(x => x.Id);
                navigationBuilder.Property(x => x.WholesaleServicesProcessId)
                    .HasConversion(processId => processId.Id, dbValue => ProcessId.Create(dbValue));
                navigationBuilder.Property(x => x.GridArea);
            });

        builder.Ignore(x => x.DomainEvents);

        builder.Property<string>("CreatedBy");
        builder.Property<Instant>("CreatedAt");
        builder.Property<string?>("ModifiedBy");
        builder.Property<Instant?>("ModifiedAt");
    }
}
