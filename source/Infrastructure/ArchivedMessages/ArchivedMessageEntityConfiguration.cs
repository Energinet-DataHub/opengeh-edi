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
using Domain.ArchivedMessages;
using Domain.Documents;
using Domain.OutgoingMessages;
using Domain.SeedWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.ArchivedMessages;

public class ArchivedMessageEntityConfiguration : IEntityTypeConfiguration<ArchivedMessage>
{
    public void Configure(EntityTypeBuilder<ArchivedMessage> builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        builder.ToTable("ArchivedMessages", "dbo");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedNever();
        builder.Property(x => x.DocumentType)
            .HasConversion(
                toDbValue => toDbValue.Name,
                fromDbValue => EnumerationType.FromName<DocumentType>(fromDbValue));
        builder.Property(x => x.SenderNumber)
            .HasConversion(
            toDbValue => toDbValue.Value,
            fromDbValue => ActorNumber.Create(fromDbValue));
        builder.Property(x => x.ReceiverNumber)
            .HasConversion(
            toDbValue => toDbValue.Value,
            fromDbValue => ActorNumber.Create(fromDbValue));
        builder.Property(x => x.CreatedAt);
        builder.Property(x => x.ProcessType)
            .HasConversion(
                toDbValue => toDbValue == null ? null : toDbValue.Name,
                fromDbValue => !string.IsNullOrWhiteSpace(fromDbValue) ? ProcessType.From(fromDbValue) : null);
    }
}
