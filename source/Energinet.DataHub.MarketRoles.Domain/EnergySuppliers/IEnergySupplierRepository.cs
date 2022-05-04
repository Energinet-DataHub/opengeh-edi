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

using System.Threading.Tasks;

namespace Energinet.DataHub.MarketRoles.Domain.EnergySuppliers
{
    /// <summary>
    /// Repository of energy suppliers
    /// </summary>
    public interface IEnergySupplierRepository
    {
        /// <summary>
        /// Checks if an energy supplier id exists
        /// </summary>
        /// <param name="glnNumber"></param>
        /// <returns><see cref="bool"/></returns>
        Task<bool> ExistsAsync(GlnNumber glnNumber);

        /// <summary>
        /// Add new energy supplier
        /// </summary>
        /// <param name="energySupplier"></param>
        void Add(EnergySupplier energySupplier);

        /// <summary>
        /// Find Energy supplier by GLN number
        /// </summary>
        /// <param name="glnNumber"></param>
        /// <returns><see cref="EnergySupplier"/></returns>
        Task<EnergySupplier?> GetByGlnNumberAsync(GlnNumber glnNumber);
    }
}
