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
using Energinet.DataHub.MarketRoles.Domain.Consumers;
using Energinet.DataHub.MarketRoles.Domain.EnergySuppliers;
using Energinet.DataHub.MarketRoles.Domain.MeteringPoints;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess.AccountingPoints;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess.Consumers;
using Energinet.DataHub.MarketRoles.Infrastructure.DataAccess.EnergySuppliers;
using Energinet.DataHub.MarketRoles.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketRoles.Infrastructure.DataAccess
{
    public class MarketRolesContext : DbContext
    {
        public MarketRolesContext(DbContextOptions<MarketRolesContext> options)
            : base(options)
        {
        }

        public MarketRolesContext()
        {
        }

        public DbSet<EnergySupplier> EnergySuppliers { get; set; } = null!;

        public DbSet<Consumer> Consumers { get; set; } = null!;

        public DbSet<AccountingPoint> AccountingPoints { get; set; } = null!;

        public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (modelBuilder == null) throw new ArgumentNullException(nameof(modelBuilder));

            modelBuilder.ApplyConfiguration(new EnergySupplierEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ConsumerEntityConfiguration());
            modelBuilder.ApplyConfiguration(new AccountingPointEntityConfiguration());
            modelBuilder.ApplyConfiguration(new OutboxMessageEntityConfiguration());
        }
    }
}
