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

using Energinet.DataHub.EDI.BuildingBlocks.Infrastructure.DataAccess;
using Energinet.DataHub.EDI.MasterData.Domain.Actors;
using Energinet.DataHub.EDI.MasterData.Domain.GridAreaOwners;
using Energinet.DataHub.EDI.MasterData.Infrastructure.ActorCertificates;
using Energinet.DataHub.EDI.MasterData.Infrastructure.Actors;
using Energinet.DataHub.EDI.MasterData.Infrastructure.GridAreas;
using Energinet.DataHub.EDI.MasterData.Infrastructure.ProcessDelegations;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.EDI.MasterData.Infrastructure.DataAccess;

public class MasterDataContext : DbContext, IEdiDbContext
{
#nullable disable
    public MasterDataContext(DbContextOptions<MasterDataContext> options)
        : base(options)
    {
    }

    public MasterDataContext()
    {
    }

    // ReSharper disable once UnusedAutoPropertyAccessor.Local
    // It is used by EF.
    public DbSet<Actor> Actors { get; private set; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Local
    // It is used by EF.
    public DbSet<GridAreaOwner> GridAreaOwners { get; private set; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Local
    // It is used by EF.
    public DbSet<Domain.ProcessDelegations.ProcessDelegation> ProcessDelegations { get; private set; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Local
    // It is used by EF.
    public DbSet<Domain.ActorCertificates.ActorCertificate> ActorCertificates { get; private set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new ActorEntityConfiguration());
        modelBuilder.ApplyConfiguration(new GridAreaOwnerEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ActorCertificateEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ProcessDelegationEntityConfiguration());
    }
}
