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
using System.Threading.Tasks;
using Energinet.DataHub.MarketData.Domain.EnergySuppliers;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketData.Infrastructure.DatabaseAccess.Write.EnergySuppliers
{
    public class EnergySupplierRepository : IEnergySupplierRepository
    {
        private readonly IWriteDatabaseContext _databaseContext;

        public EnergySupplierRepository(IWriteDatabaseContext databaseContext)
        {
            _databaseContext = databaseContext;
        }

        public async Task<bool> ExistsAsync(GlnNumber glnNumber)
        {
            if (glnNumber is null)
            {
                throw new ArgumentNullException(nameof(glnNumber));
            }

            return await _databaseContext.EnergySupplierDataModels.AnyAsync(e => e.MrId == glnNumber.Value);
        }

        public void Add(EnergySupplier energySupplier)
        {
            if (energySupplier is null)
            {
                throw new ArgumentNullException(nameof(energySupplier));
            }

            var snapshot = energySupplier.GetSnapshot();
            var dataModel = GetDataModelFrom(snapshot);

            _databaseContext.EnergySupplierDataModels.Add(dataModel);
        }

        private static EnergySupplierDataModel GetDataModelFrom(EnergySupplierSnapshot snapshot)
        {
            return new EnergySupplierDataModel(snapshot.Id, snapshot.GlnNumber, snapshot.Version);
        }
    }
}
