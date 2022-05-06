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
using System.Threading.Tasks;
using Energinet.DataHub.MarketRoles.Domain.EnergySuppliers;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketRoles.Infrastructure.DataAccess.EnergySuppliers
{
    public class EnergySupplierRepository : IEnergySupplierRepository
    {
        private readonly MarketRolesContext _context;

        public EnergySupplierRepository(MarketRolesContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public Task<bool> ExistsAsync(GlnNumber glnNumber)
        {
            if (glnNumber == null) throw new ArgumentNullException(nameof(glnNumber));
            return _context.EnergySuppliers.AnyAsync(x => x.GlnNumber.Equals(glnNumber));
        }

        public void Add(EnergySupplier energySupplier)
        {
            if (energySupplier == null) throw new ArgumentNullException(nameof(energySupplier));
            _context.EnergySuppliers.Add(energySupplier);
        }

        public Task<EnergySupplier?> GetByGlnNumberAsync(GlnNumber glnNumber)
        {
            if (glnNumber == null) throw new ArgumentNullException(nameof(glnNumber));
            return _context.EnergySuppliers
                .Where(x => x.GlnNumber.Equals(glnNumber))
                .SingleOrDefaultAsync();
        }
    }
}
