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
using System.Collections.Generic;
using Energinet.DataHub.EDI.Domain.Actors;
using Energinet.DataHub.EDI.Domain.OutgoingMessages;
using Energinet.DataHub.EDI.Domain.Transactions;
using Energinet.DataHub.EDI.Domain.Transactions.AggregatedMeasureData;
using Energinet.DataHub.EDI.Domain.Transactions.Aggregations;
using Energinet.DataHub.EDI.Infrastructure.OutgoingMessages.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using NodaTime;
using ActorGrouping = Energinet.DataHub.EDI.Domain.Transactions.AggregatedMeasureData.ActorGrouping;
using GridAreaDetails = Energinet.DataHub.EDI.Domain.Transactions.AggregatedMeasureData.GridAreaDetails;
using Period = Energinet.DataHub.EDI.Domain.Transactions.AggregatedMeasureData.Period;
using Point = Energinet.DataHub.EDI.Domain.Transactions.AggregatedMeasureData.Point;

namespace Energinet.DataHub.EDI.Infrastructure.Transactions.AggregatedMeasureData;

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
                dbValue => BusinessReason.FromCode(dbValue));
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
                dbValue => !string.IsNullOrWhiteSpace(dbValue) ? SettlementVersion.FromCode(dbValue) : null);

        builder.OwnsMany<PendingAggregation>("_pendingAggregations", navigationBuilder =>
        {
            navigationBuilder.ToTable("PendingAggregations", "dbo");
            navigationBuilder.HasKey("Id");
            navigationBuilder.WithOwner().HasForeignKey("ProcessId");
            navigationBuilder.Property("Id");
            navigationBuilder.Property(x => x.Resolution);
            navigationBuilder.Property(x => x.MeteringPointType)
                .HasConversion(
                    toDbValue => toDbValue.Code,
                    fromDbValue => MeteringPointType.FromCode(fromDbValue));

            navigationBuilder.Property(x => x.SettlementType)
                .HasConversion(
                    toDbValue
                        => toDbValue != null ? toDbValue.Code : null,
                    fromDbValue
                        => fromDbValue != null ? SettlementType.From(fromDbValue) : null);

            navigationBuilder.Property(x => x.SettlementVersion)
                .HasConversion(
                    toDbValue
                        => toDbValue != null ? toDbValue.Code : null,
                    fromDbValue
                        => fromDbValue != null ? SettlementVersion.FromCode(fromDbValue) : null);

            navigationBuilder.Property(x => x.BusinessReason)
                .HasConversion(
                    toDbValue
                        => toDbValue.Code,
                    fromDbValue
                        => BusinessReason.FromCode(fromDbValue));

            navigationBuilder.Property(x => x.MeasurementUnit)
                .HasConversion(
                    toDbValue
                        => toDbValue.Code,
                    fromDbValue
                        => MeasurementUnit.From(fromDbValue));

            navigationBuilder.Property(x => x.BusinessTransactionId)
                .HasConversion(
                    toDbValue
                        => toDbValue != null ? toDbValue.Id : null,
                    fromDbValue
                        => fromDbValue != null ? BusinessTransactionId.Create(fromDbValue) : null);

            navigationBuilder.Property(x => x.ReceiverId)
                .HasConversion(
                    toDbValue
                        => toDbValue != null ? toDbValue.Value : null,
                    fromDbValue
                        => fromDbValue != null ? ActorNumber.Create(fromDbValue) : null);

            navigationBuilder.Property(x => x.ReceiverRole)
                .HasConversion(
                    toDbValue
                        => toDbValue != null ? toDbValue.Code : null,
                    fromDbValue
                        => fromDbValue != null ? MarketRole.FromCode(fromDbValue) : null);

            navigationBuilder.Property(x => x.ProcessId)
                .HasConversion(
                    toDbValue => toDbValue.Id,
                    fromDbValue => ProcessId.Create(fromDbValue));

            navigationBuilder.Property(x => x.Points)
                .HasConversion(
                    toDbValue => JsonConvert.SerializeObject(toDbValue),
                    fromDbValue =>
                        JsonConvert.DeserializeObject<IReadOnlyList<Point>>(fromDbValue) ?? new List<Point>());

            navigationBuilder.OwnsOne<Period>(x => x.Period, period
                =>
            {
                period.Property(y => y.Start).HasColumnName("PeriodStart");
                period.Property(y => y.End).HasColumnName("PeriodEnd");
            });

            navigationBuilder.OwnsOne<GridAreaDetails>(x => x.GridAreaDetails, gridDetails
                =>
            {
                gridDetails.Property(y => y.GridAreaCode).HasColumnName("GridAreaCode");
                gridDetails.Property(y => y.OperatorNumber).HasColumnName("GridAreaResponsibleId");
            });
        });

        builder.HasMany<OutgoingMessage>("_messages")
            .WithOne()
            .HasForeignKey("ProcessId");

        builder.Ignore(x => x.DomainEvents);
    }
}
