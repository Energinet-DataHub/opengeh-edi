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
        builder.Property(x => x.SettlementSeriesVersion);
        builder.Property(x => x.MarketEvaluationPointType);
        builder.Property(x => x.MarketEvaluationSettlementMethod);
        builder.Property(x => x.StartDateAndOrTimeDateTime);
        builder.Property(x => x.EndDateAndOrTimeDateTime);
        builder.Property(x => x.MeteringGridAreaDomainId);
        builder.Property(x => x.BiddingZoneDomainId);
        builder.Property(x => x.EnergySupplierMarketParticipantId);
        builder.Property(x => x.BalanceResponsiblePartyMarketParticipantId);

        builder.Property<ActorNumber>("_requestedByActorId")
            .HasColumnName("RequestedByActorId")
            .HasConversion(
                toDbValue => toDbValue.Value,
                fromDbValue => ActorNumber.Create(fromDbValue));

        builder.Property<AggregatedMeasureDataProcess.State>("_state")
            .HasConversion(toDbValue => toDbValue.ToString(), fromDbValue => Enum.Parse<AggregatedMeasureDataProcess.State>(fromDbValue, true))
            .HasColumnName("State");

        builder.Ignore(x => x.DomainEvents);
    }
}
