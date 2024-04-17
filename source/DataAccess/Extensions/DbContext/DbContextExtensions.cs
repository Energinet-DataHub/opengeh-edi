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
using System.Linq;
using Energinet.DataHub.EDI.BuildingBlocks.Domain.Authentication;
using Energinet.DataHub.EDI.BuildingBlocks.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.EDI.DataAccess.Extensions.DbContext;

public static class DbContextExtensions
{
    public static void UpdateAuditFields(
        this Microsoft.EntityFrameworkCore.DbContext dbContext,
        BuildingBlocks.Domain.ExecutionContext executionContext,
        AuthenticatedActor authenticatedActor,
        ISystemDateTimeProvider systemDateTimeProvider)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(executionContext);
        ArgumentNullException.ThrowIfNull(authenticatedActor);
        ArgumentNullException.ThrowIfNull(systemDateTimeProvider);

        var modifiedEntries = dbContext.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in modifiedEntries)
        {
            var entityType = entry.Context.Model.FindEntityType(entry.Entity.GetType());

            if (entry.State == EntityState.Added
                && entityType?.FindProperty("CreatedBy") != null
                && entityType.FindProperty("CreatedAt") != null)
            {
                entry.Property("CreatedBy").CurrentValue =
                    authenticatedActor.TryGetCurrentActorIdentity(out var actorIdentity)
                        ? actorIdentity?.ActorNumber.Value
                        : executionContext.CurrentExecutionType.Name;
                entry.Property("CreatedAt").CurrentValue = systemDateTimeProvider.Now();
            }

            if (entry.State == EntityState.Modified
                && entityType?.FindProperty("ModifiedBy") != null
                && entityType.FindProperty("ModifiedAt") != null)
            {
                entry.Property("ModifiedBy").CurrentValue =
                    authenticatedActor.TryGetCurrentActorIdentity(out var actorIdentity)
                        ? actorIdentity?.ActorNumber.Value
                        : executionContext.CurrentExecutionType.Name;
                entry.Property("ModifiedAt").CurrentValue = systemDateTimeProvider.Now();
            }
        }
    }
}
